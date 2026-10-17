using Microsoft.EntityFrameworkCore;
using Raphael.Api.Realtime;
using Raphael.Api.Services.Notifications;
using Raphael.Api.Services.Trips;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Routing
{
    /// <summary>A garage change that cannot be carried onto the planned days. Answered as a 409.</summary>
    public sealed class GarageChangeRefusedException : Exception
    {
        /// <summary>A drive between the new garage and the route could not be measured.</summary>
        public const string LegUnavailable = "garage_leg_unavailable";

        /// <summary>The route or its planned days moved since the preview the user confirmed.</summary>
        public const string ChangedSincePreview = "route_changed_since_preview";

        public GarageChangeRefusedException(string message, string code = LegUnavailable) : base(message) => Code = code;

        public string Code { get; }
    }

    /// <summary>What was staged on the context, and who has to hear about it once it is saved.</summary>
    public sealed class GarageChangePlan
    {
        public GarageChangePreviewDto Result { get; } = new();
        internal List<(DateTime Date, Trip? AnyTrip, bool DriverIsOut)> Touched { get; } = new();
    }

    public interface IGarageChangeService
    {
        /// <summary>What the change would do. Nothing is saved.</summary>
        Task<GarageChangePreviewDto> PreviewAsync(int vehicleRouteId, GarageChangeRequestDto request, CancellationToken ct);

        /// <summary>
        /// Carries the new garage onto every planned day and re-chains them, on the tracked rows.
        /// The caller saves, inside its own transaction, then calls <see cref="NotifyAsync"/>.
        /// </summary>
        Task<GarageChangePlan> StageAsync(int vehicleRouteId, GarageChangeRequestDto request, CancellationToken ct);

        Task NotifyAsync(int vehicleRouteId, GarageChangePlan plan);
    }

    /// <summary>
    /// Changing a route's garage, carried onto the days already planned on it (RE-052).
    /// </summary>
    /// <remarks>
    /// The Pull-out and Pull-in of a day are created once, with the garage the route had then, and
    /// nothing used to touch them again: a new garage left every planned day leaving from and
    /// returning to the old one. Now, for every day from today on:
    /// <list type="bullet">
    /// <item>A <b>past</b> day is history and is not touched (RE-051).</item>
    /// <item>A day whose vehicle is <b>back</b> (Pull-in performed) is finished: not touched.</item>
    /// <item>A day whose vehicle has <b>left</b> (Pull-out performed) keeps its Pull-out — it left
    /// from where it left — and its Pull-in goes to the new garage.</item>
    /// <item>Any other day gets both.</item>
    /// </list>
    /// Each touched day is then re-chained <b>whole</b>, exactly as a fresh dispatch screen would:
    /// a provisional chain to settle the hours, every pending leg priced at the hour it is driven,
    /// the chain again, and the server's own derivation of waits and garage hour last. The garage
    /// legs usually cancel out of every arrival — the Pull-out moves by as much as the first drive
    /// changes — but not when the departure runs into midnight, nor when the first stop promises
    /// no hour, nor when a leg changes hour of the day; walking the whole route is the only way not
    /// to have to know in advance which of those it is.
    /// </remarks>
    public sealed class GarageChangeService : IGarageChangeService
    {
        private const int MaxLegsPerRequest = 100;

        private readonly RaphaelContext _context;
        private readonly IRoutingService _routing;
        private readonly IScheduleService _schedules;
        private readonly ITripChangeGuard _guard;
        private readonly ITripNotificationPublisher _notifications;
        private readonly IDispatchBroadcaster _board;
        private readonly IOperationClock _clock;

        public GarageChangeService(RaphaelContext context, IRoutingService routing, IScheduleService schedules,
            ITripChangeGuard guard, ITripNotificationPublisher notifications, IDispatchBroadcaster board, IOperationClock clock)
        {
            _clock = clock;
            _context = context;
            _routing = routing;
            _schedules = schedules;
            _guard = guard;
            _notifications = notifications;
            _board = board;
        }

        public async Task<GarageChangePreviewDto> PreviewAsync(int vehicleRouteId, GarageChangeRequestDto request, CancellationToken ct)
        {
            try
            {
                var plan = await StageAsync(vehicleRouteId, request, ct);
                return plan.Result;
            }
            finally
            {
                // Everything staged is thrown away: a preview writes nothing.
                _context.ChangeTracker.Clear();
            }
        }

        public async Task<GarageChangePlan> StageAsync(int vehicleRouteId, GarageChangeRequestDto request, CancellationToken ct)
        {
            var plan = new GarageChangePlan();

            var route = await _context.VehicleRoutes.AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == vehicleRouteId, ct)
                ?? throw new KeyNotFoundException("VehicleRoute not found.");

            plan.Result.GarageChanged =
                !string.Equals((route.Garage ?? "").Trim(), (request.Garage ?? "").Trim(), StringComparison.Ordinal)
                || !Near(route.GarageLatitude, request.GarageLatitude)
                || !Near(route.GarageLongitude, request.GarageLongitude);

            if (!plan.Result.GarageChanged) return plan;

            var providerId = route.OwnerProviderId;

            var garageDays = await _context.Schedules.AsNoTracking()
                .Where(s => s.VehicleRouteId == vehicleRouteId && s.Date != null
                            && (s.Name == "Pull-out" || s.Name == "Pull-in"))
                .Select(s => s.Date!.Value.Date)
                .Distinct()
                .ToListAsync(ct);

            plan.Result.PastDaysUntouched = garageDays.Count(d => _guard.IsPastDay(d, providerId));

            var days = garageDays.Where(d => !_guard.IsPastDay(d, providerId)).OrderBy(d => d).ToList();

            plan.Result.Fingerprint = await FingerprintAsync(route, days, ct);

            // ⚠️ Two phases, and the order is the point. The routing service saves its cache on this
            // same context, so nothing of ours may be tracked while legs are being priced — or a
            // preview would write the new garage to the database, and a save would write half a
            // route before the rest of it was even worked out. Phase 1 chains and prices on
            // untracked copies; phase 2, with no more calls out, writes onto tracked rows.
            var chained = new List<(DateTime Day, List<ChainStop> Stops)>();
            foreach (var day in days)
            {
                var stops = await ChainDayAsync(vehicleRouteId, day, request, ct);
                chained.Add((day, stops));
            }

            foreach (var (day, stops) in chained)
            {
                plan.Result.Days.Add(await ApplyDayAsync(vehicleRouteId, day, providerId, request, stops, plan, ct));
            }

            return plan;
        }

        /// <summary>
        /// A digest of everything the answer depends on: the route's garage and, for each planned day,
        /// every row's order, hours, drive and progress. Read before anything is staged.
        /// </summary>
        private async Task<string> FingerprintAsync(VehicleRoute route, List<DateTime> days, CancellationToken ct)
        {
            var text = new System.Text.StringBuilder()
                .Append(route.Garage).Append('|').Append(route.GarageLatitude.ToString("R"))
                .Append('|').Append(route.GarageLongitude.ToString("R"));

            foreach (var day in days)
            {
                text.Append('#').Append(day.ToString("yyyyMMdd"));
                var rows = await DayRows(route.Id, day).AsNoTracking()
                    .Select(s => new
                    {
                        s.Id, s.Sequence, s.ETATime, s.TravelTime, s.Performed, s.ActualArriveTime,
                        s.ScheduledPickupTime, s.ScheduledApptTime,
                        Cancelled = s.Trip != null && s.Trip.IsCancelled
                    })
                    .ToListAsync(ct);

                foreach (var r in rows.OrderBy(r => r.Id))
                {
                    text.Append(';').Append(r.Id).Append(',').Append(r.Sequence).Append(',').Append(r.ETATime)
                        .Append(',').Append(r.TravelTime).Append(',').Append(r.Performed).Append(',').Append(r.ActualArriveTime)
                        .Append(',').Append(r.ScheduledPickupTime).Append(',').Append(r.ScheduledApptTime).Append(',').Append(r.Cancelled);
                }
            }

            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString()));
            return Convert.ToHexString(hash);
        }

        /// <summary>The rows the dispatch screen chains, in its order: GetSchedulesByRouteAndDateAsync's.</summary>
        private IQueryable<Schedule> DayRows(int vehicleRouteId, DateTime day)
        {
            var next = day.AddDays(1);
            return _context.Schedules
                .Where(s => s.VehicleRouteId == vehicleRouteId && s.Date >= day && s.Date < next)
                .Where(s => s.Trip == null || s.Trip.IsCancelled == false || s.EventType == ScheduleEventType.Pickup)
                .OrderBy(s => s.Sequence).ThenBy(s => s.ETATime);
        }

        /// <summary>
        /// Phase 1: the day re-chained with the new garage, on copies. Null when the day is finished.
        /// </summary>
        private async Task<List<ChainStop>?> ChainDayAsync(int vehicleRouteId, DateTime day,
            GarageChangeRequestDto request, CancellationToken ct)
        {
            var rows = await DayRows(vehicleRouteId, day).AsNoTracking().Include(s => s.Trip).ToListAsync(ct);

            if (rows.FirstOrDefault(r => r.Name == "Pull-in")?.Performed == true) return null;

            var stops = rows.Select(ToChainStop).ToList();

            foreach (var stop in stops)
            {
                if ((stop.IsPullOut || stop.IsPullIn) && !stop.Performed)
                {
                    stop.Latitude = request.GarageLatitude;
                    stop.Longitude = request.GarageLongitude;
                }
            }

            // As a recalculation on screen: a provisional chain so every leg is priced at the hour it
            // will really be driven, the prices, then the chain that is kept.
            RouteEtaChain.Chain(stops);
            await PriceAsync(stops, day, ct);
            RouteEtaChain.Chain(stops);
            return stops;
        }

        /// <summary>Phase 2: the chained day onto the tracked rows, the server's derivation, and what moved.</summary>
        private async Task<GarageChangeDayDto> ApplyDayAsync(int vehicleRouteId, DateTime day, int? providerId,
            GarageChangeRequestDto request, List<ChainStop>? stops, GarageChangePlan plan, CancellationToken ct)
        {
            var rows = await DayRows(vehicleRouteId, day).Include(s => s.Trip).ToListAsync(ct);

            var pullOut = rows.FirstOrDefault(r => r.Name == "Pull-out");
            var pullIn = rows.FirstOrDefault(r => r.Name == "Pull-in");

            var result = new GarageChangeDayDto
            {
                Date = day,
                IsToday = day.Date == _clock.TodayFor(providerId).Date,
                PullOutAlreadyLeft = pullOut?.Performed == true,
                DayFinished = stops == null,
                PullOutEtaBefore = pullOut?.ETATime,
                PullInEtaBefore = pullIn?.ETATime,
                FirstLegBefore = FirstLegRow(rows)?.TravelTime,
                LastLegBefore = pullIn?.TravelTime
            };

            if (stops == null)
            {
                result.PullOutEtaAfter = result.PullOutEtaBefore;
                result.PullInEtaAfter = result.PullInEtaBefore;
                result.FirstLegAfter = result.FirstLegBefore;
                result.LastLegAfter = result.LastLegBefore;
                return result;
            }

            // Somebody moved the route between the two phases: the chain no longer describes it.
            if (rows.Count != stops.Count || !rows.Select(r => r.Id).ToHashSet().SetEquals(stops.Select(s => s.Id)))
                throw new GarageChangeRefusedException(
                    "The route changed while the new garage was being worked out. Try again; nothing was changed.");

            var before = rows.ToDictionary(r => r.Id, r => (r.ETATime, r.EarlyArrivalWait));

            if (pullOut != null && !pullOut.Performed)
            {
                SetGarage(pullOut, request);
                result.PullOutUpdated = true;
            }

            if (pullIn != null && !pullIn.Performed)
            {
                SetGarage(pullIn, request);
                result.PullInUpdated = true;
            }

            WriteBack(rows, stops);

            // The server's last word on the garage hour and the waits, as after any reorder.
            await _schedules.RederiveRouteDayAsync(vehicleRouteId, day);

            result.PullOutEtaAfter = pullOut?.ETATime;
            result.PullInEtaAfter = pullIn?.ETATime;
            result.FirstLegAfter = FirstLegRow(rows)?.TravelTime;
            result.LastLegAfter = pullIn?.TravelTime;

            foreach (var row in rows)
            {
                if (row.Name == "Pull-out" || row.Name == "Pull-in") continue;
                var (etaBefore, waitBefore) = before[row.Id];

                // Compared as the dispatcher reads them: in whole minutes. A few seconds' drift
                // inside the same minute is not a different hour on anybody's screen.
                if (Minutes(etaBefore) == Minutes(row.ETATime) && Minutes(waitBefore) == Minutes(row.EarlyArrivalWait))
                    continue;

                result.ChangedStops.Add(new GarageChangeStopDto
                {
                    ScheduleId = row.Id,
                    Name = row.Name,
                    EtaBefore = etaBefore,
                    EtaAfter = row.ETATime,
                    WaitBefore = waitBefore,
                    WaitAfter = row.EarlyArrivalWait
                });
            }

            plan.Touched.Add((day, rows.FirstOrDefault(r => r.Trip != null)?.Trip, result.PullOutAlreadyLeft));
            return result;
        }

        /// <summary>The first stop the vehicle drives to from the garage, among tracked rows.</summary>
        private static Schedule? FirstLegRow(List<Schedule> rows)
        {
            var stops = rows.Select(ToChainStop).ToList();
            var first = FirstLeg(stops);
            return first == null ? null : rows.First(r => r.Id == first.Id);
        }

        /// <summary>Prices every leg a fresh screen would, and refuses if a garage leg cannot be.</summary>
        private async Task PriceAsync(List<ChainStop> stops, DateTime day, CancellationToken ct)
        {
            var legs = new List<ChainLeg>();

            foreach (var leg in RouteEtaChain.LegsToPrice(stops, day))
            {
                // A route-day with no trips left chains the Pull-in straight from the Pull-out: garage
                // to the same garage. That is no drive at all, and asking for it could only fail.
                if (Near(leg.From.Latitude, leg.To.Latitude) && Near(leg.From.Longitude, leg.To.Longitude))
                {
                    leg.To.Travel = TimeSpan.Zero;
                    leg.To.Distance = 0;
                    continue;
                }

                legs.Add(leg);
            }

            for (int start = 0; start < legs.Count; start += MaxLegsPerRequest)
            {
                var chunk = legs.Skip(start).Take(MaxLegsPerRequest).ToList();

                var response = await _routing.GetLegsAsync(new RouteLegsRequestDto
                {
                    Legs = chunk.Select(l => new RouteLegRequestItemDto
                    {
                        OriginLat = l.From.Latitude,
                        OriginLng = l.From.Longitude,
                        DestLat = l.To.Latitude,
                        DestLng = l.To.Longitude,
                        Date = l.Date,
                        DepartureTime = l.Departure
                    }).ToList()
                }, ct);

                for (int i = 0; i < chunk.Count; i++)
                {
                    var leg = chunk[i];
                    var priced = i < response.Legs.Count ? response.Legs[i] : null;
                    var usable = priced != null && priced.Status == RoutingContract.Statuses.Ok;

                    if (!usable)
                    {
                        // ⚠️ Any other leg keeps the drive it had, as on screen. A garage leg cannot:
                        // its old figure measures the drive to a garage the route no longer has, and
                        // an hour built on it would be a believable wrong hour for the driver.
                        if (leg.From.IsPullOut || leg.To.IsPullIn)
                            throw new GarageChangeRefusedException(
                                "The drive between the new garage and the route could not be measured. " +
                                "Check the garage address and try again; nothing was changed.");
                        continue;
                    }

                    leg.To.Distance = priced!.DistanceMiles;
                    leg.To.Travel = TimeSpan.FromSeconds(priced.DurationInTrafficSeconds ?? priced.DurationSeconds);
                }
            }
        }

        /// <summary>The chained figures back onto the rows, clamped as every server write is.</summary>
        private static void WriteBack(List<Schedule> rows, List<ChainStop> stops)
        {
            // By id, never by position: two stops that tie on order may come back the other way round.
            var byId = stops.ToDictionary(s => s.Id);

            foreach (var row in rows)
            {
                var stop = byId[row.Id];

                if (row.Name == "Pull-out")
                {
                    // Sequence 0 always, as the reorder pins it. Its hour is re-derived after this.
                    row.Sequence = 0;
                    if (!row.Performed && stop.Eta.HasValue) row.ETATime = RouteEtaChain.ClampToDayEnds(stop.Eta.Value);
                    continue;
                }

                row.Sequence = stop.Sequence;
                row.ETATime = stop.Eta.HasValue ? RouteEtaChain.ClampToDayEnds(stop.Eta.Value) : null;
                row.TravelTime = stop.Travel;
                row.DistanceToPoint = stop.Distance;
            }
        }

        public async Task NotifyAsync(int vehicleRouteId, GarageChangePlan plan)
        {
            foreach (var (date, anyTrip, driverIsOut) in plan.Touched)
            {
                // Other dispatchers with this day open reload it.
                await _board.RouteChangedAsync(vehicleRouteId, date);

                // A driver already out reloads the route they are driving. The publisher checks
                // that they really have left; before Pull-out they see it when they start.
                if (driverIsOut && anyTrip != null)
                    await _notifications.DriverRouteUpdatedAsync(anyTrip, RouteChangeTypes.Updated, vehicleRouteId);
            }
        }

        private static void SetGarage(Schedule row, GarageChangeRequestDto request)
        {
            row.Address = request.Garage;
            row.ScheduleLatitude = request.GarageLatitude;
            row.ScheduleLongitude = request.GarageLongitude;
        }

        private static ChainStop ToChainStop(Schedule s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            EventType = s.EventType,
            TripId = s.TripId,
            Status = s.Trip == null ? null : (s.Trip.IsCancelled ? "Canceled" : s.Trip.Status),
            TripType = s.Trip?.Type,
            Pickup = s.ScheduledPickupTime,
            Appt = s.ScheduledApptTime,
            Performed = s.Performed,
            Perform = s.ActualPerformTime,
            Arrive = s.ActualArriveTime,
            Date = s.Date,
            Latitude = s.ScheduleLatitude,
            Longitude = s.ScheduleLongitude,
            Sequence = s.Sequence,
            Eta = s.ETATime,
            Travel = s.TravelTime,
            Distance = s.DistanceToPoint
        };

        /// <summary>The first stop the vehicle drives to from the garage.</summary>
        private static ChainStop? FirstLeg(List<ChainStop> stops)
        {
            for (int i = 1; i < stops.Count; i++)
            {
                if (stops[i].IsCancelledUnvisited) continue;
                return RouteEtaChain.FindValidPrevious(stops, i)?.IsPullOut == true ? stops[i] : null;
            }
            return null;
        }

        private static long? Minutes(TimeSpan? t) => t.HasValue ? (long)Math.Floor(t.Value.TotalMinutes) : null;

        private static bool Near(double a, double b) => Math.Abs(a - b) < 0.000001;
    }
}
