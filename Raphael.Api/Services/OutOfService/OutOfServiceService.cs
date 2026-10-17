using Microsoft.EntityFrameworkCore;
using Raphael.Api.Realtime;
using Raphael.Api.Services.Notifications;
using Raphael.Api.Services.Trips;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Services;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Definitions.OutOfService;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Realtime;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.OutOfService
{
    /// <summary>An Out of Service request that cannot be carried out. Answered as a 409, or a 400 when the request is malformed.</summary>
    public sealed class OutOfServiceRefusedException : Exception
    {
        public const string Invalid = "out_of_service_invalid";

        public const string AlreadyOut = "already_out_of_service";

        public const string SubstitutionRefused = "substitution_refused";

        /// <summary>The affected trips changed since the preview the user confirmed.</summary>
        public const string ChangedSincePreview = "changed_since_preview";

        public OutOfServiceRefusedException(string message, string code) : base(message) => Code = code;

        public string Code { get; }

        public bool IsBadRequest => Code == Invalid;
    }

    public interface IOutOfServiceService
    {
        /// <summary>What taking these subjects out of service would do. Nothing is saved.</summary>
        Task<OutOfServicePreviewDto> PreviewAsync(OutOfServiceRequestDto request, CancellationToken ct);

        /// <summary>Does it, in one transaction, if nothing changed since the preview.</summary>
        Task<OutOfServicePreviewDto> ExecuteAsync(OutOfServiceRequestDto request, CancellationToken ct);

        /// <summary>Puts one subject back in service. Its trips do not come back.</summary>
        Task<OutOfServiceRecordDto> ReturnToServiceAsync(int recordId, CancellationToken ct);

        /// <summary>Every vehicle and driver out of service now.</summary>
        Task<List<OutOfServiceRecordDto>> GetActiveAsync(CancellationToken ct);
    }

    /// <summary>
    /// Taking a vehicle, a driver or both out of service (RE-053).
    /// </summary>
    /// <remarks>
    /// From the moment it is confirmed, every route of the subject stops taking trips, and every trip
    /// still pending on it — today and on any later day — goes back to the unscheduled list:
    /// <list type="bullet">
    /// <item>A trip <b>finished</b> (both events performed) is history: not touched.</item>
    /// <item>A trip with the patient <b>on board</b> (Pickup performed, Dropoff not) stays on the
    /// route so it can be finished, and is recorded as <c>EXCLUDED_ONBOARD</c>.</item>
    /// <item>A trip on a route another vehicle or driver <b>takes over</b> stays on it.</item>
    /// <item>Any other trip is taken off, exactly as the Schedule tab's unroute does.</item>
    /// </list>
    /// All of it, records included, is one transaction: it happens whole or not at all. The notices
    /// go out after the commit.
    /// </remarks>
    public sealed class OutOfServiceService : IOutOfServiceService
    {
        private readonly RaphaelContext _context;
        private readonly IScheduleService _schedules;
        private readonly ITripChangeGuard _guard;
        private readonly ITripNotificationPublisher _tripNotifications;
        private readonly NotificationService _notifications;
        private readonly IDispatchBroadcaster _board;
        private readonly IOperationClock _clock;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<OutOfServiceService> _logger;

        public OutOfServiceService(RaphaelContext context, IScheduleService schedules, ITripChangeGuard guard,
            ITripNotificationPublisher tripNotifications, NotificationService notifications, IDispatchBroadcaster board,
            IOperationClock clock, ICurrentUserService currentUser, ILogger<OutOfServiceService> logger)
        {
            _context = context;
            _schedules = schedules;
            _guard = guard;
            _tripNotifications = tripNotifications;
            _notifications = notifications;
            _board = board;
            _clock = clock;
            _currentUser = currentUser;
            _logger = logger;
        }

        // ---- The plan: what would happen, worked out the same way for the preview and the execution ----

        private sealed class PlannedTrip
        {
            public required Trip Trip { get; init; }
            public required List<Schedule> Events { get; init; }
            public required string Outcome { get; init; }
        }

        private sealed class PlannedRoute
        {
            public required VehicleRoute Route { get; init; }
            public OutOfServiceSubstitutionDto? Substitution { get; init; }
            public string? NewVehicleName { get; init; }
            public string? NewDriverName { get; init; }
            public List<PlannedTrip> Trips { get; } = new();
        }

        private sealed class Plan
        {
            public Vehicle? Vehicle { get; set; }
            public User? Driver { get; set; }
            public OutOfServiceSubjectDto? VehicleSubject { get; set; }
            public OutOfServiceSubjectDto? DriverSubject { get; set; }
            public List<PlannedRoute> Routes { get; } = new();
            public string Fingerprint { get; set; } = string.Empty;
        }

        public async Task<OutOfServicePreviewDto> PreviewAsync(OutOfServiceRequestDto request, CancellationToken ct)
        {
            var plan = await BuildPlanAsync(request, ct);
            return ToDto(plan, operationId: null);
        }

        public async Task<OutOfServicePreviewDto> ExecuteAsync(OutOfServiceRequestDto request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Fingerprint))
                throw new OutOfServiceRefusedException("Confirm the preview first.", OutOfServiceRefusedException.Invalid);

            var operationId = Guid.NewGuid();
            var now = _clock.UtcNow;
            Plan plan;
            var records = new List<OutOfServiceRecord>();
            var unrouted = new List<(Trip Trip, int VehicleRouteId)>();

            await using (var transaction = await _context.Database.BeginTransactionAsync(ct))
            {
                plan = await BuildPlanAsync(request, ct);

                if (!string.Equals(plan.Fingerprint, request.Fingerprint, StringComparison.Ordinal))
                    throw new OutOfServiceRefusedException(
                        "The trips of these routes changed since the preview. Review the new preview; nothing was changed.",
                        OutOfServiceRefusedException.ChangedSincePreview);

                if (plan.VehicleSubject is not null)
                    records.Add(NewRecord(operationId, plan.VehicleSubject, plan.Vehicle!.OwnerProviderId, request.OriginVehicleRouteId, now));
                if (plan.DriverSubject is not null)
                    records.Add(NewRecord(operationId, plan.DriverSubject, plan.Vehicle?.OwnerProviderId
                        ?? plan.Routes.FirstOrDefault()?.Route.OwnerProviderId, request.OriginVehicleRouteId, now));

                _context.OutOfServiceRecords.AddRange(records);

                try
                {
                    // Saved first: the unique index on the open record is what stops two dispatchers
                    // taking the same vehicle out at once. The loser fails here, before touching a trip.
                    await _context.SaveChangesAsync(ct);
                }
                catch (DbUpdateException)
                {
                    throw new OutOfServiceRefusedException(
                        "Somebody else has just taken it out of service. Nothing was changed.",
                        OutOfServiceRefusedException.AlreadyOut);
                }

                foreach (var planned in plan.Routes)
                {
                    var record = RecordFor(planned.Route, records);

                    if (planned.Substitution is { } sub)
                    {
                        var route = await _context.VehicleRoutes.FirstAsync(r => r.Id == planned.Route.Id, ct);
                        if (sub.NewVehicleId is int v) route.VehicleId = v;
                        if (sub.NewDriverId is int d) route.DriverId = d;
                    }

                    foreach (var trip in planned.Trips)
                    {
                        record.TripImpacts.Add(new OutOfServiceTripImpact
                        {
                            TripId = trip.Trip.Id,
                            VehicleRouteId = planned.Route.Id,
                            Date = trip.Trip.Date.Date,
                            Outcome = trip.Outcome,
                            OldVehicleId = planned.Route.VehicleId,
                            NewVehicleId = planned.Substitution?.NewVehicleId,
                            OldDriverId = planned.Route.DriverId,
                            NewDriverId = planned.Substitution?.NewDriverId
                        });

                        if (trip.Outcome == OutOfServiceTripOutcomes.Unrouted)
                        {
                            var done = await _schedules.UnrouteTripInTransactionAsync(trip.Trip.Id, planned.Route.Id);
                            if (done is not null) unrouted.Add((done, planned.Route.Id));
                        }
                    }
                }

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
            }

            await NotifyOutOfServiceAsync(operationId, plan, unrouted);
            return ToDto(plan, operationId);
        }

        private OutOfServiceRecord NewRecord(Guid operationId, OutOfServiceSubjectDto subject, int? ownerProviderId, int? originRouteId, DateTime now) => new()
        {
            OperationId = operationId,
            Subject = subject.Subject,
            SubjectId = subject.SubjectId,
            ReasonCode = subject.ReasonCode,
            ReasonNote = string.IsNullOrWhiteSpace(subject.ReasonNote) ? null : subject.ReasonNote.Trim(),
            OriginVehicleRouteId = originRouteId,
            StartedAtUtc = now,
            StartedByUserId = _currentUser.UserId,
            StartedByName = Truncate(_currentUser.UserName),
            OwnerProviderId = ownerProviderId
        };

        /// <summary>The record a route's trips are told against: its vehicle's when the vehicle is out, its driver's otherwise.</summary>
        private static OutOfServiceRecord RecordFor(VehicleRoute route, List<OutOfServiceRecord> records) =>
            records.FirstOrDefault(r => r.Subject == OutOfServiceSubjects.Vehicle && r.SubjectId == route.VehicleId)
            ?? records.FirstOrDefault(r => r.Subject == OutOfServiceSubjects.Driver && r.SubjectId == route.DriverId)
            ?? records[0];

        private async Task<Plan> BuildPlanAsync(OutOfServiceRequestDto request, CancellationToken ct)
        {
            var plan = new Plan();
            ValidateSubjects(request, plan);

            if (plan.VehicleSubject is { } vs)
            {
                plan.Vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vs.SubjectId, ct)
                    ?? throw new KeyNotFoundException("Vehicle not found.");
            }

            if (plan.DriverSubject is { } ds)
            {
                plan.Driver = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ds.SubjectId, ct)
                    ?? throw new KeyNotFoundException("Driver not found.");
            }

            var vehicleId = plan.Vehicle?.Id ?? -1;
            var driverId = plan.Driver?.Id ?? -1;

            if (await RouteServiceStatus.OpenRecordsFor(_context, vehicleId, driverId).AnyAsync(ct))
                throw new OutOfServiceRefusedException(
                    "It is already out of service. Put it back in service first.",
                    OutOfServiceRefusedException.AlreadyOut);

            var routes = await _context.VehicleRoutes.AsNoTracking()
                .Include(r => r.Vehicle)
                .Include(r => r.Driver)
                .Where(r => r.VehicleId == vehicleId || r.DriverId == driverId)
                .OrderBy(r => r.Name)
                .ToListAsync(ct);

            var substitutions = request.Substitutions ?? new List<OutOfServiceSubstitutionDto>();
            if (substitutions.Any(s => routes.All(r => r.Id != s.VehicleRouteId)))
                throw new OutOfServiceRefusedException("A substitution names a route that is not affected.", OutOfServiceRefusedException.Invalid);
            if (substitutions.GroupBy(s => s.VehicleRouteId).Any(g => g.Count() > 1))
                throw new OutOfServiceRefusedException("A route can only be substituted once.", OutOfServiceRefusedException.Invalid);

            foreach (var route in routes)
            {
                var sub = substitutions.FirstOrDefault(s => s.VehicleRouteId == route.Id);
                var planned = sub is null
                    ? new PlannedRoute { Route = route }
                    : await PlanSubstitutionAsync(route, sub, vehicleId, driverId, ct);

                await PlanTripsAsync(planned, ct);

                // A route with nothing pending is still shown: it is out of service from now on.
                plan.Routes.Add(planned);
            }

            await ValidateCapacityAsync(plan, ct);
            plan.Fingerprint = Fingerprint(request, plan);
            return plan;
        }

        private static void ValidateSubjects(OutOfServiceRequestDto request, Plan plan)
        {
            var subjects = request.Subjects ?? new List<OutOfServiceSubjectDto>();
            if (subjects.Count is < 1 or > 2)
                throw new OutOfServiceRefusedException("Choose the vehicle, the driver or both.", OutOfServiceRefusedException.Invalid);

            foreach (var s in subjects)
            {
                if (!OutOfServiceSubjects.IsValid(s.Subject))
                    throw new OutOfServiceRefusedException($"Unknown subject {s.Subject}.", OutOfServiceRefusedException.Invalid);
                if (!OutOfServiceReasonCodes.IsValid(s.Subject, s.ReasonCode))
                    throw new OutOfServiceRefusedException("Choose a reason from the list.", OutOfServiceRefusedException.Invalid);
                if (s.ReasonCode == OutOfServiceReasonCodes.Other && string.IsNullOrWhiteSpace(s.ReasonNote))
                    throw new OutOfServiceRefusedException("\"Other\" needs a note that says why.", OutOfServiceRefusedException.Invalid);
                if (s.ReasonNote is { Length: > OutOfServiceReasonCodes.NoteMaxLength })
                    throw new OutOfServiceRefusedException(
                        $"The note can be {OutOfServiceReasonCodes.NoteMaxLength} characters long at most.", OutOfServiceRefusedException.Invalid);

                if (s.Subject == OutOfServiceSubjects.Vehicle)
                {
                    if (plan.VehicleSubject is not null)
                        throw new OutOfServiceRefusedException("Only one vehicle at a time.", OutOfServiceRefusedException.Invalid);
                    plan.VehicleSubject = s;
                }
                else
                {
                    if (plan.DriverSubject is not null)
                        throw new OutOfServiceRefusedException("Only one driver at a time.", OutOfServiceRefusedException.Invalid);
                    plan.DriverSubject = s;
                }
            }
        }

        /// <summary>
        /// Another vehicle and/or driver for one route. Refused unless, with it, the route is back in
        /// service: replacing a sick driver of a broken van keeps the van broken.
        /// </summary>
        private async Task<PlannedRoute> PlanSubstitutionAsync(VehicleRoute route, OutOfServiceSubstitutionDto sub,
            int outVehicleId, int outDriverId, CancellationToken ct)
        {
            var newVehicleId = sub.NewVehicleId ?? route.VehicleId;
            var newDriverId = sub.NewDriverId ?? route.DriverId;

            if (newVehicleId == outVehicleId || newDriverId == outDriverId)
                throw new OutOfServiceRefusedException(
                    $"Route {route.Name} would still be out of service: give it a vehicle and a driver that are in service.",
                    OutOfServiceRefusedException.SubstitutionRefused);

            if (await RouteServiceStatus.OpenRecordsFor(_context, newVehicleId, newDriverId).AnyAsync(ct))
                throw new OutOfServiceRefusedException(
                    $"The vehicle or driver chosen for route {route.Name} is out of service itself.",
                    OutOfServiceRefusedException.SubstitutionRefused);

            string? vehicleName = null, driverName = null;

            if (sub.NewVehicleId is int v)
            {
                var vehicle = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == v, ct)
                    ?? throw new KeyNotFoundException("Substitute vehicle not found.");
                if (vehicle.IsInactive)
                    throw new OutOfServiceRefusedException($"Vehicle {vehicle.Name} is inactive.", OutOfServiceRefusedException.SubstitutionRefused);
                vehicleName = vehicle.Name;
            }

            if (sub.NewDriverId is int d)
            {
                var driver = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == d, ct)
                    ?? throw new KeyNotFoundException("Substitute driver not found.");
                if (!driver.IsActive)
                    throw new OutOfServiceRefusedException($"Driver {driver.FullName} is inactive.", OutOfServiceRefusedException.SubstitutionRefused);
                driverName = driver.FullName;
            }

            // One vehicle and one driver work one route a day. The substitute must be free on every day
            // this route still has something planned.
            var today = _clock.TodayFor(route.OwnerProviderId).Date;
            var days = await _context.Schedules.AsNoTracking()
                .Where(s => s.VehicleRouteId == route.Id && s.Date >= today)
                .Select(s => s.Date!.Value.Date).Distinct().ToListAsync(ct);

            if (days.Count > 0)
            {
                var busy = await _context.Schedules.AsNoTracking()
                    .Where(s => s.VehicleRouteId != route.Id && s.Date >= today && days.Contains(s.Date!.Value.Date)
                                && ((sub.NewVehicleId != null && s.VehicleRoute.VehicleId == sub.NewVehicleId)
                                    || (sub.NewDriverId != null && s.VehicleRoute.DriverId == sub.NewDriverId)))
                    .Select(s => s.VehicleRoute.Name)
                    .FirstOrDefaultAsync(ct);

                if (busy is not null)
                    throw new OutOfServiceRefusedException(
                        $"The vehicle or driver chosen for route {route.Name} already works route {busy} on one of its days.",
                        OutOfServiceRefusedException.SubstitutionRefused);
            }

            return new PlannedRoute { Route = route, Substitution = sub, NewVehicleName = vehicleName, NewDriverName = driverName };
        }

        /// <summary>The route's trips from today on, and what happens to each.</summary>
        private async Task PlanTripsAsync(PlannedRoute planned, CancellationToken ct)
        {
            var today = _clock.TodayFor(planned.Route.OwnerProviderId).Date;

            var events = await _context.Schedules.AsNoTracking()
                .Include(s => s.Trip).ThenInclude(t => t!.Customer)
                .Where(s => s.VehicleRouteId == planned.Route.Id && s.Date >= today && s.TripId != null)
                .ToListAsync(ct);

            foreach (var group in events.GroupBy(s => s.TripId!.Value).OrderBy(g => g.First().Date).ThenBy(g => g.Min(e => e.ETATime)))
            {
                var trip = group.First().Trip!;

                // A cancelled trip waiting on the route, or a closed one, is not a pending trip.
                if (trip.IsCancelled || trip.Status == TripStatus.Canceled || _guard.IsReadOnly(trip)) continue;

                var pickup = group.FirstOrDefault(e => e.EventType == ScheduleEventType.Pickup);
                var dropoff = group.FirstOrDefault(e => e.EventType == ScheduleEventType.Dropoff);

                var outcome = Classify(pickup?.Performed == true, dropoff?.Performed == true, planned.Substitution is not null);
                if (outcome is null) continue;

                planned.Trips.Add(new PlannedTrip
                {
                    Trip = trip,
                    Events = group.OrderBy(e => e.Sequence).ThenBy(e => e.ETATime).ToList(),
                    Outcome = outcome
                });
            }
        }

        /// <summary>
        /// What happens to one trip of the route: null when it is finished and is not touched.
        /// </summary>
        /// <remarks>
        /// ⚠️ The patient on board wins over everything, substitution included: that trip is being
        /// driven right now, by this vehicle, and taking it off would leave a patient in a van that
        /// no screen shows.
        /// </remarks>
        public static string? Classify(bool pickupPerformed, bool dropoffPerformed, bool routeSubstituted)
        {
            if (pickupPerformed && dropoffPerformed) return null;
            if (pickupPerformed) return OutOfServiceTripOutcomes.ExcludedOnboard;
            return routeSubstituted ? OutOfServiceTripOutcomes.KeptBySubstitution : OutOfServiceTripOutcomes.Unrouted;
        }

        /// <summary>
        /// A substitute vehicle must carry what the kept trips need: a van without a lift cannot take
        /// a wheelchair. Same capacity type as the vehicle it replaces is enough; otherwise every
        /// kept trip's space type must be in the new vehicle's capacity.
        /// </summary>
        private async Task ValidateCapacityAsync(Plan plan, CancellationToken ct)
        {
            foreach (var planned in plan.Routes.Where(r => r.Substitution?.NewVehicleId != null))
            {
                var newVehicle = await _context.Vehicles.AsNoTracking().FirstAsync(v => v.Id == planned.Substitution!.NewVehicleId, ct);
                if (newVehicle.CapacityDetailTypeId == planned.Route.Vehicle.CapacityDetailTypeId) continue;

                var needed = planned.Trips.Where(t => t.Outcome == OutOfServiceTripOutcomes.KeptBySubstitution)
                    .Select(t => t.Trip.SpaceTypeId).Distinct().ToList();
                if (needed.Count == 0) continue;

                var carried = await _context.CapacityDetails.AsNoTracking()
                    .Where(c => c.CapacityDetailTypeId == newVehicle.CapacityDetailTypeId && c.Quantity > 0 && needed.Contains(c.SpaceTypeId))
                    .Select(c => c.SpaceTypeId).Distinct().ToListAsync(ct);

                if (needed.Except(carried).Any())
                    throw new OutOfServiceRefusedException(
                        $"Vehicle {newVehicle.Name} cannot carry every trip of route {planned.Route.Name}: " +
                        "one of them needs a space this vehicle does not have.",
                        OutOfServiceRefusedException.SubstitutionRefused);
            }
        }

        /// <summary>Everything the answer depends on: the subjects, the substitutions, and each trip's events and progress.</summary>
        /// <remarks>
        /// ⚠️ Not the ETAs. The driver's app moves them all day long on a live route, and with them in
        /// here almost every confirmation of today would be refused as "changed since the preview".
        /// What decides the outcome of a trip is which events exist and which are performed.
        /// </remarks>
        private static string Fingerprint(OutOfServiceRequestDto request, Plan plan)
        {
            var text = new System.Text.StringBuilder();
            foreach (var s in (request.Subjects ?? new()).OrderBy(s => s.Subject))
                text.Append(s.Subject).Append(':').Append(s.SubjectId).Append(';');

            foreach (var route in plan.Routes.OrderBy(r => r.Route.Id))
            {
                text.Append('#').Append(route.Route.Id).Append(',').Append(route.Route.VehicleId).Append(',').Append(route.Route.DriverId)
                    .Append(',').Append(route.Substitution?.NewVehicleId).Append(',').Append(route.Substitution?.NewDriverId);

                foreach (var trip in route.Trips.OrderBy(t => t.Trip.Id))
                {
                    text.Append('|').Append(trip.Trip.Id).Append(',').Append(trip.Outcome);
                    foreach (var e in trip.Events.OrderBy(e => e.Id))
                        text.Append(',').Append(e.Id).Append(':').Append(e.Performed);
                }
            }

            var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString()));
            return Convert.ToHexString(hash);
        }

        private static OutOfServicePreviewDto ToDto(Plan plan, Guid? operationId)
        {
            var dto = new OutOfServicePreviewDto { Fingerprint = plan.Fingerprint, OperationId = operationId };

            foreach (var planned in plan.Routes)
            {
                var route = new OutOfServiceRouteDto
                {
                    VehicleRouteId = planned.Route.Id,
                    RouteName = planned.Route.Name,
                    VehicleId = planned.Route.VehicleId,
                    VehicleName = planned.Route.Vehicle?.Name,
                    DriverId = planned.Route.DriverId,
                    DriverName = planned.Route.Driver?.FullName,
                    NewVehicleId = planned.Substitution?.NewVehicleId,
                    NewVehicleName = planned.NewVehicleName,
                    NewDriverId = planned.Substitution?.NewDriverId,
                    NewDriverName = planned.NewDriverName
                };

                foreach (var trip in planned.Trips)
                {
                    route.Trips.Add(new OutOfServiceTripDto
                    {
                        TripId = trip.Trip.Id,
                        Date = trip.Trip.Date.Date,
                        PatientName = trip.Trip.Customer?.FullName,
                        Outcome = trip.Outcome,
                        Events = trip.Events.Select(e => new OutOfServiceEventDto
                        {
                            ScheduleId = e.Id,
                            EventType = e.EventType?.ToString() ?? string.Empty,
                            Name = e.Name,
                            Address = e.Address,
                            ScheduledTime = e.EventType == ScheduleEventType.Pickup ? e.ScheduledPickupTime : e.ScheduledApptTime,
                            Eta = e.ETATime,
                            Performed = e.Performed
                        }).ToList()
                    });
                }

                dto.Routes.Add(route);
            }

            var all = dto.Routes.SelectMany(r => r.Trips).ToList();
            var gone = all.Where(t => t.Outcome == OutOfServiceTripOutcomes.Unrouted).ToList();
            dto.TripsUnrouted = gone.Count;
            dto.EventsUnrouted = gone.Sum(t => t.Events.Count);
            dto.TripsOnboard = all.Count(t => t.Outcome == OutOfServiceTripOutcomes.ExcludedOnboard);
            dto.TripsKept = all.Count(t => t.Outcome == OutOfServiceTripOutcomes.KeptBySubstitution);
            dto.UnroutedDates = gone.Select(t => t.Date).Distinct().OrderBy(d => d).ToList();
            return dto;
        }

        // ---- Return to service, and the list ----

        public async Task<OutOfServiceRecordDto> ReturnToServiceAsync(int recordId, CancellationToken ct)
        {
            var record = await _context.OutOfServiceRecords.FirstOrDefaultAsync(r => r.Id == recordId, ct)
                ?? throw new KeyNotFoundException("Out of Service record not found.");

            if (record.EndedAtUtc is not null)
                throw new OutOfServiceRefusedException("It is already back in service.", OutOfServiceRefusedException.Invalid);

            record.EndedAtUtc = _clock.UtcNow;
            record.EndedByUserId = _currentUser.UserId;
            record.EndedByName = Truncate(_currentUser.UserName);
            await _context.SaveChangesAsync(ct);

            var routes = await RoutesOf(record).ToListAsync(ct);
            var (vehicleName, driverName) = await SubjectNamesAsync(new[] { record }, ct);

            var data = BaseData(record.OperationId, record.OwnerProviderId, routes.Select(r => r.Name));
            data[BusinessEventDataKeys.OutOfServiceCause] = record.Subject;
            if (vehicleName is not null) data[BusinessEventDataKeys.VehicleName] = vehicleName;
            if (driverName is not null) data[BusinessEventDataKeys.DriverName] = driverName;

            await PublishAsync(BusinessEventCodes.RouteReturnedToService, record.OperationId, data);
            await _board.RouteServiceChangedAsync(new RouteServiceChangedMessage
            {
                OperationId = record.OperationId,
                VehicleRouteIds = routes.Select(r => r.Id).ToList(),
                OutOfService = false
            }, record.OwnerProviderId);

            return (await ToRecordDtosAsync(new List<OutOfServiceRecord> { record }, ct))[0];
        }

        public async Task<List<OutOfServiceRecordDto>> GetActiveAsync(CancellationToken ct)
        {
            var open = await _context.OutOfServiceRecords.AsNoTracking()
                .Where(r => r.EndedAtUtc == null)
                .OrderBy(r => r.StartedAtUtc)
                .ToListAsync(ct);

            return await ToRecordDtosAsync(open, ct);
        }

        private async Task<List<OutOfServiceRecordDto>> ToRecordDtosAsync(List<OutOfServiceRecord> records, CancellationToken ct)
        {
            var vehicleIds = records.Where(r => r.Subject == OutOfServiceSubjects.Vehicle).Select(r => r.SubjectId).ToList();
            var driverIds = records.Where(r => r.Subject == OutOfServiceSubjects.Driver).Select(r => r.SubjectId).ToList();

            var vehicles = await _context.Vehicles.AsNoTracking().Where(v => vehicleIds.Contains(v.Id))
                .ToDictionaryAsync(v => v.Id, v => v.Name, ct);
            var drivers = await _context.Users.AsNoTracking().Where(u => driverIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

            return records.Select(r => new OutOfServiceRecordDto
            {
                Id = r.Id,
                OperationId = r.OperationId,
                Subject = r.Subject,
                SubjectId = r.SubjectId,
                SubjectName = r.Subject == OutOfServiceSubjects.Vehicle
                    ? vehicles.GetValueOrDefault(r.SubjectId)
                    : drivers.GetValueOrDefault(r.SubjectId),
                ReasonCode = r.ReasonCode,
                ReasonNote = r.ReasonNote,
                StartedAtUtc = DateTime.SpecifyKind(r.StartedAtUtc, DateTimeKind.Utc),
                StartedByName = r.StartedByName,
                EndedAtUtc = r.EndedAtUtc is { } e ? DateTime.SpecifyKind(e, DateTimeKind.Utc) : null,
                EndedByName = r.EndedByName
            }).ToList();
        }

        private IQueryable<VehicleRoute> RoutesOf(OutOfServiceRecord record) =>
            _context.VehicleRoutes.AsNoTracking().Where(r =>
                (record.Subject == OutOfServiceSubjects.Vehicle && r.VehicleId == record.SubjectId)
                || (record.Subject == OutOfServiceSubjects.Driver && r.DriverId == record.SubjectId));

        private async Task<(string? Vehicle, string? Driver)> SubjectNamesAsync(IEnumerable<OutOfServiceRecord> records, CancellationToken ct)
        {
            var dtos = await ToRecordDtosAsync(records.ToList(), ct);
            return (dtos.FirstOrDefault(d => d.Subject == OutOfServiceSubjects.Vehicle)?.SubjectName,
                    dtos.FirstOrDefault(d => d.Subject == OutOfServiceSubjects.Driver)?.SubjectName);
        }

        // ---- After the commit: every screen, every driver, and the office ----

        private async Task NotifyOutOfServiceAsync(Guid operationId, Plan plan, List<(Trip Trip, int VehicleRouteId)> unrouted)
        {
            // Each trip taken off tells its driver, as the unroute does, and the clinic following it.
            // The dispatch screens are told once below, not once per trip: see RouteServiceChangedMessage.
            foreach (var (trip, routeId) in unrouted)
            {
                await _tripNotifications.DriverRouteUpdatedAsync(trip, RouteChangeTypes.Removed, routeId);
                await _board.TripTrackingChangedAsync(trip.Id);
            }

            // The route a dispatcher may have open lost stops: one reload per route and day.
            foreach (var (routeId, day) in unrouted.Select(u => (u.VehicleRouteId, u.Trip.Date.Date)).Distinct())
                await _board.RouteChangedAsync(routeId, day);

            // A route that changed hands keeps its trips but not its vehicle or driver: every day it
            // has planned is reloaded, by the screens and by the phone working it.
            foreach (var planned in plan.Routes.Where(r => r.Substitution is not null))
            {
                foreach (var day in planned.Trips.GroupBy(t => t.Trip.Date.Date))
                {
                    await _board.RouteChangedAsync(planned.Route.Id, day.Key);
                    await _tripNotifications.DriverRouteUpdatedAsync(day.First().Trip, RouteChangeTypes.Updated, planned.Route.Id);
                }
            }

            var dto = ToDto(plan, operationId);
            var ownerProviderId = plan.Vehicle?.OwnerProviderId ?? plan.Routes.FirstOrDefault()?.Route.OwnerProviderId;

            await _board.RouteServiceChangedAsync(new RouteServiceChangedMessage
            {
                OperationId = operationId,
                VehicleRouteIds = plan.Routes.Select(r => r.Route.Id).ToList(),
                OutOfService = true,
                UnroutedTripIds = unrouted.Select(u => u.Trip.Id).ToList(),
                UnroutedDates = unrouted.Select(u => u.Trip.Date.Date).Distinct().OrderBy(d => d).ToList()
            }, ownerProviderId);

            var data = BaseData(operationId, ownerProviderId, plan.Routes.Select(r => r.Route.Name));
            data[BusinessEventDataKeys.OutOfServiceCause] = plan.VehicleSubject is not null && plan.DriverSubject is not null ? "BOTH"
                : plan.VehicleSubject is not null ? OutOfServiceSubjects.Vehicle : OutOfServiceSubjects.Driver;
            if (plan.Vehicle is not null) data[BusinessEventDataKeys.VehicleName] = plan.Vehicle.Name;
            if (plan.Driver is not null) data[BusinessEventDataKeys.DriverName] = plan.Driver.FullName;
            if (plan.VehicleSubject is not null) data[BusinessEventDataKeys.VehicleReasonCode] = plan.VehicleSubject.ReasonCode;
            if (plan.DriverSubject is not null) data[BusinessEventDataKeys.DriverReasonCode] = plan.DriverSubject.ReasonCode;
            data[BusinessEventDataKeys.TripsUnrouted] = dto.TripsUnrouted;
            data[BusinessEventDataKeys.TripsOnboard] = dto.TripsOnboard;
            data[BusinessEventDataKeys.TripsKept] = dto.TripsKept;

            await PublishAsync(BusinessEventCodes.RouteOutOfService, operationId, data);
        }

        /// <summary>
        /// The payload both events share. ⚠️ Codes, names of vehicles and drivers, counts: never the
        /// note, which may say why a driver is on leave, and never a patient.
        /// </summary>
        private Dictionary<string, object?> BaseData(Guid operationId, int? ownerProviderId, IEnumerable<string> routeNames)
        {
            var data = new Dictionary<string, object?>
            {
                [BusinessEventDataKeys.OutOfServiceOperationId] = operationId.ToString(),
                [BusinessEventDataKeys.RouteNames] = string.Join(", ", routeNames)
            };

            // A client company's fleet: its own office hears it, not Raphael's (RE-050).
            if (ownerProviderId is int tenant && _context.Providers.IgnoreQueryFilters().Any(p => p.Id == tenant && p.IsTenant))
                data[BusinessEventDataKeys.AudienceTenantId] = tenant;

            if (_currentUser.UserId.HasValue)
                data[BusinessEventDataKeys.PerformedByUserId] = _currentUser.UserId.Value;

            return data;
        }

        /// <summary>Never lets a notice that fails undo an Out of Service that went through.</summary>
        private async Task PublishAsync(string eventCode, Guid operationId, Dictionary<string, object?> data)
        {
            try
            {
                await _notifications.PublishAsync(
                    eventCode: eventCode,
                    aggregateId: operationId,
                    data: data,
                    performedByUserId: _currentUser.UserId.HasValue
                        ? UserIdentifierConverter.ToGuid(_currentUser.UserId.Value, RecipientType.DesktopUser)
                        : null,
                    cancellationToken: CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish {EventCode} for Out of Service operation {OperationId}.", eventCode, operationId);
            }
        }

        private static string? Truncate(string? value) =>
            value is null ? null : value.Length <= 150 ? value : value[..150];
    }
}
