using Raphael.Shared.Entities;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// One row of a route-day as the chain sees it: what the dispatch screen's grid holds.
    /// </summary>
    /// <remarks>
    /// Built from exactly the rows <c>ScheduleService.GetSchedulesByRouteAndDateAsync</c> hands the
    /// Desktop, in the same order, with <see cref="Status"/> reading "Canceled" whenever the trip is
    /// cancelled — because that list is what <c>SchedulesViewModel.ChainEtas</c> walks.
    /// </remarks>
    public sealed class ChainStop
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public ScheduleEventType? EventType { get; init; }
        public int? TripId { get; init; }
        public string? Status { get; init; }
        public string? TripType { get; init; }

        /// <summary>The promised pickup hour (<c>ScheduledPickupTime</c>).</summary>
        public TimeSpan? Pickup { get; init; }

        /// <summary>The promised appointment hour (<c>ScheduledApptTime</c>).</summary>
        public TimeSpan? Appt { get; init; }

        public bool Performed { get; init; }
        public TimeSpan? Perform { get; init; }
        public TimeSpan? Arrive { get; init; }
        public DateTime? Date { get; init; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public int? Sequence { get; set; }
        public TimeSpan? Eta { get; set; }
        public TimeSpan? Travel { get; set; }
        public double? Distance { get; set; }

        public bool IsPullOut => Name == "Pull-out";
        public bool IsPullIn => Name == "Pull-in";

        /// <summary>A cancellation nobody drove to: no leg, no distance, no hour of its own.</summary>
        public bool IsCancelledUnvisited => Status == "Canceled" && Arrive == null;
    }

    /// <summary>A leg the chain needs priced: from one stop to the next, leaving at a given hour.</summary>
    public sealed record ChainLeg(ChainStop From, ChainStop To, DateTime Date, TimeSpan Departure);

    /// <summary>
    /// The arrival-time chain of a route-day, on the server (RE-052).
    /// </summary>
    /// <remarks>
    /// ⚠️ A faithful port of <c>SchedulesViewModel.ChainEtas</c>, <c>SyncPullOutEta</c>,
    /// <c>FindValidPrevious</c>, <c>DepartureTimeOf</c>, <c>PlannedDepartureFrom</c> and the leg
    /// selection of <c>FetchChangedTravelTimesAsync</c> in Raphael.Desktop. It exists because a change
    /// of garage has to re-chain every day it touches at the moment it is saved, without a dispatch
    /// screen open on each of them.
    ///
    /// <para>
    /// Two copies of one rule can drift apart, which is what <c>Raphael.Api.Tests</c> is for: every
    /// case there was worked out by hand from the Desktop's code. Change one side and the other in the
    /// same slice, and the tests with them.
    /// </para>
    ///
    /// <para>
    /// The waits are not worked out here. As after any reorder, the server derives them from the
    /// chained hours with <c>ScheduleService.ApplyDerivedRouteValuesAsync</c>, which has the last word.
    /// </para>
    /// </remarks>
    public static class RouteEtaChain
    {
        /// <summary>How long the vehicle needs at the garage before it can leave.</summary>
        public const int PullOutPreparationMinutes = 20;

        /// <summary>How long a vehicle stands at a stop. <c>ScheduleDto.On</c> is never filled, so always this.</summary>
        public const int StopServiceMinutes = 15;

        /// <summary>
        /// Chains the whole route-day: the garage hour first, then every stop after the Pull-out.
        /// </summary>
        public static void Chain(IReadOnlyList<ChainStop> stops)
        {
            if (stops.Count == 0) return;

            SyncPullOutEta(stops);

            for (int i = 1; i < stops.Count; i++)
            {
                var current = stops[i];
                var previous = FindValidPrevious(stops, i) ?? stops[0];

                current.Sequence = i;

                if (current.IsCancelledUnvisited)
                {
                    current.Distance = 0;
                    current.Travel = TimeSpan.Zero;
                    current.Eta = DepartureTimeOf(previous);
                }
                else if (!current.Performed)
                {
                    var eta = PlannedDepartureFrom(previous) + (current.Travel ?? TimeSpan.Zero);

                    // The early-arrival rule: never shown arriving more than a quarter of an hour
                    // before a promised pickup — five minutes on a return.
                    if (current.EventType == ScheduleEventType.Pickup && current.Pickup.HasValue)
                    {
                        var margin = EarlyArrivalMarginFor(current.TripType);
                        if (eta < current.Pickup.Value - margin) eta = current.Pickup.Value - margin;
                    }

                    current.Eta = eta;
                }
            }
        }

        /// <summary>
        /// Every leg a fresh dispatch screen would price on its first recalculation: each pending,
        /// physical stop, from the stop the vehicle leaves to reach it, at the hour it leaves.
        /// </summary>
        /// <remarks>
        /// All of them, not only the garage's: the Desktop's memory of what it already measured is
        /// per screen and starts empty, so the first recalculation of any session asks for every
        /// leg. Asking for the same set is what makes this route and the one the next dispatcher
        /// recalculates the same route. The server's cache answers the ones that did not change.
        /// </remarks>
        public static List<ChainLeg> LegsToPrice(IReadOnlyList<ChainStop> stops, DateTime day)
        {
            var legs = new List<ChainLeg>();

            for (int i = 1; i < stops.Count; i++)
            {
                var current = stops[i];

                if (current.Performed) continue;
                if (current.IsCancelledUnvisited) continue;

                var previous = FindValidPrevious(stops, i);
                if (previous == null) continue;

                var departure = PlannedDepartureFrom(previous);

                // Same fallback as the screen: with no estimate ahead, price the leg at this stop's
                // own hour rather than at midnight.
                if (departure == TimeSpan.Zero)
                    departure = current.Pickup ?? current.Appt ?? TimeSpan.Zero;

                legs.Add(new ChainLeg(previous, current, current.Date ?? day, departure));
            }

            return legs;
        }

        /// <summary>
        /// Re-derives the garage departure hour from the first real stop.
        /// </summary>
        /// <remarks>
        /// Not once the driver has left, and not once anything at all on the route is performed:
        /// from then on the departure is history.
        /// </remarks>
        public static void SyncPullOutEta(IReadOnlyList<ChainStop> stops)
        {
            var pullOut = stops.FirstOrDefault(s => s.IsPullOut);
            if (pullOut == null || pullOut.Performed) return;
            if (stops.Any(s => s.Performed)) return;

            var firstStop = stops.FirstOrDefault(s =>
                s.TripId.HasValue && !s.IsPullIn && (s.Status != "Canceled" || s.Arrive != null));
            if (firstStop == null) return;

            var commitment = firstStop.Pickup ?? firstStop.Appt;
            if (commitment == null) return;

            pullOut.Eta = ClampToDayEnds(
                commitment.Value
                - TimeSpan.FromMinutes(PullOutPreparationMinutes)
                - (firstStop.Travel ?? TimeSpan.Zero));
        }

        /// <summary>The stop the vehicle left to reach the one at <paramref name="index"/>.</summary>
        public static ChainStop? FindValidPrevious(IReadOnlyList<ChainStop> stops, int index)
        {
            for (int j = index - 1; j >= 0; j--)
            {
                var p = stops[j];
                if (p.Status != "Canceled" || p.Arrive != null) return p;
            }

            return stops.Count > 0 ? stops[0] : null;
        }

        /// <summary>When the vehicle left a stop: what really happened if it has, the estimate if not.</summary>
        public static TimeSpan DepartureTimeOf(ChainStop? stop)
        {
            if (stop == null) return TimeSpan.Zero;
            if (stop.Performed && stop.Perform.HasValue) return stop.Perform.Value;
            if (stop.Arrive.HasValue) return stop.Arrive.Value;
            return stop.Eta ?? TimeSpan.Zero;
        }

        /// <summary>The hour the vehicle leaves a stop for the next one: arrival plus the time spent there.</summary>
        public static TimeSpan PlannedDepartureFrom(ChainStop? stop)
        {
            if (stop == null) return TimeSpan.Zero;

            var service = stop.IsPullOut ? TimeSpan.Zero : TimeSpan.FromMinutes(StopServiceMinutes);
            return DepartureTimeOf(stop) + service;
        }

        public static TimeSpan EarlyArrivalMarginFor(string? tripType) =>
            tripType == "Return" ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(15);

        /// <summary>Pins an hour that ran off either end of the day to that end, as every server write does.</summary>
        public static TimeSpan ClampToDayEnds(TimeSpan value)
        {
            if (value < TimeSpan.Zero) return TimeSpan.Zero;
            var endOfDay = new TimeSpan(23, 59, 59);
            return value > endOfDay ? endOfDay : value;
        }
    }
}
