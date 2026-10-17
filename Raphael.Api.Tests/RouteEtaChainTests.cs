using Raphael.Api.Services.Routing;
using Raphael.Shared.Entities;

namespace Raphael.Api.Tests
{
    /// <summary>
    /// The server's copy of the dispatch screen's arrival chain (RE-052).
    /// </summary>
    /// <remarks>
    /// ⚠️ Every expected hour here was worked out by hand from <c>SchedulesViewModel.ChainEtas</c>,
    /// <c>SyncPullOutEta</c>, <c>FindValidPrevious</c>, <c>DepartureTimeOf</c> and
    /// <c>PlannedDepartureFrom</c> in Raphael.Desktop, with the figures written next to each case. If
    /// one of these fails, the server and the screen disagree about a driver's day: fix whichever side
    /// moved, never the expectation alone.
    /// </remarks>
    public class RouteEtaChainTests
    {
        private static readonly DateTime Day = new(2026, 10, 20);

        private static TimeSpan H(int h, int m) => new(h, m, 0);
        private static TimeSpan M(int m) => TimeSpan.FromMinutes(m);

        private static ChainStop PullOut(TimeSpan? eta = null, bool performed = false, TimeSpan? perform = null) =>
            new() { Id = 1, Name = "Pull-out", Eta = eta ?? H(7, 0), Travel = TimeSpan.Zero, Performed = performed, Perform = perform, Date = Day };

        private static ChainStop PullIn(int id, TimeSpan travel) =>
            new() { Id = id, Name = "Pull-in", Pickup = H(23, 0), Appt = H(23, 0), Travel = travel, Date = Day };

        private static ChainStop Pickup(int id, int tripId, TimeSpan? promised, TimeSpan travel, string type = "Appointment",
            string status = "Scheduled", bool performed = false, TimeSpan? perform = null, TimeSpan? arrive = null) =>
            new()
            {
                Id = id, Name = $"Patient {tripId} Pickup - {type}", EventType = ScheduleEventType.Pickup, TripId = tripId,
                Pickup = promised, TripType = type, Status = status, Travel = travel, Performed = performed,
                Perform = perform, Arrive = arrive, Date = Day
            };

        private static ChainStop Dropoff(int id, int tripId, TimeSpan? appt, TimeSpan travel, string type = "Appointment") =>
            new()
            {
                Id = id, Name = $"Patient {tripId} Dropoff - {type}", EventType = ScheduleEventType.Dropoff, TripId = tripId,
                Appt = appt, TripType = type, Status = "Scheduled", Travel = travel, Date = Day
            };

        [Fact]
        public void A_normal_route_chains_from_the_derived_garage_hour()
        {
            // Pull-out = 08:00 − 20 − 30 = 07:10. P1 = 07:10 + 30 = 07:40, raised to 08:00 − 15 = 07:45.
            // D1 = 07:45 + 15 + 20 = 08:20. Pull-in = 08:20 + 15 + 25 = 09:00.
            var stops = new List<ChainStop>
            {
                PullOut(), Pickup(2, 10, H(8, 0), M(30)), Dropoff(3, 10, H(9, 0), M(20)), PullIn(4, M(25))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(H(7, 10), stops[0].Eta);
            Assert.Equal(H(7, 45), stops[1].Eta);
            Assert.Equal(H(8, 20), stops[2].Eta);
            Assert.Equal(H(9, 0), stops[3].Eta);
            Assert.Equal(new int?[] { 1, 2, 3 }, stops.Skip(1).Select(s => s.Sequence).ToArray());
        }

        [Fact]
        public void A_new_garage_further_away_moves_only_the_garage_hours_when_nothing_runs_into_midnight()
        {
            // The garage legs cancel out of the first arrival: the Pull-out leaves earlier by as much as
            // the drive grew. 08:00 − 20 − 50 = 06:50; P1 = 06:50 + 50 = 07:40 → 07:45 as before.
            // Back to the garage in 10 instead of 25: Pull-in = 08:35 + 10 = 08:45.
            var stops = new List<ChainStop>
            {
                PullOut(), Pickup(2, 10, H(8, 0), M(50)), Dropoff(3, 10, H(9, 0), M(20)), PullIn(4, M(10))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(H(6, 50), stops[0].Eta);
            Assert.Equal(H(7, 45), stops[1].Eta);
            Assert.Equal(H(8, 20), stops[2].Eta);
            Assert.Equal(H(8, 45), stops[3].Eta);
        }

        [Fact]
        public void A_departure_that_runs_into_midnight_is_pinned_and_the_whole_route_moves()
        {
            // 00:30 − 20 − 40 = −00:30 → pinned to 00:00. P1 = 00:00 + 40 = 00:40, after 00:15: not
            // raised, and late. D1 = 00:40 + 15 + 20 = 01:15. This is the case where a garage change
            // reaches every stop.
            var stops = new List<ChainStop>
            {
                PullOut(), Pickup(2, 10, H(0, 30), M(40)), Dropoff(3, 10, H(2, 0), M(20)), PullIn(4, M(10))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(TimeSpan.Zero, stops[0].Eta);
            Assert.Equal(H(0, 40), stops[1].Eta);
            Assert.Equal(H(1, 15), stops[2].Eta);
            Assert.Equal(H(1, 40), stops[3].Eta);
        }

        [Fact]
        public void A_return_leg_is_held_five_minutes_before_the_hour_not_fifteen()
        {
            // 10:00 − 20 − 30 = 09:10; P1 = 09:40, raised to 10:00 − 5 = 09:55.
            var stops = new List<ChainStop>
            {
                PullOut(), Pickup(2, 10, H(10, 0), M(30), type: "Return"), Dropoff(3, 10, null, M(20), type: "Return"), PullIn(4, M(15))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(H(9, 10), stops[0].Eta);
            Assert.Equal(H(9, 55), stops[1].Eta);
            Assert.Equal(H(10, 30), stops[2].Eta);
        }

        [Fact]
        public void A_cancellation_nobody_drove_to_is_skipped_by_the_chain_and_by_the_garage_hour()
        {
            // The first trip is cancelled, so the garage hour is set by the second: 09:00 − 20 − 25 = 08:15.
            // The cancelled stop takes the hour the vehicle left the previous one and no drive.
            // P2 chains from the Pull-out: 08:15 + 25 = 08:40, raised to 08:45.
            var stops = new List<ChainStop>
            {
                PullOut(),
                Pickup(2, 10, H(8, 0), M(30), status: "Canceled"),
                Pickup(3, 11, H(9, 0), M(25)),
                PullIn(4, M(15))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(H(8, 15), stops[0].Eta);
            Assert.Equal(H(8, 15), stops[1].Eta);
            Assert.Equal(TimeSpan.Zero, stops[1].Travel);
            Assert.Equal(0d, stops[1].Distance);
            Assert.Equal(H(8, 45), stops[2].Eta);
            Assert.Equal(H(9, 15), stops[3].Eta);
        }

        [Fact]
        public void A_cancellation_the_driver_did_reach_stays_a_stop()
        {
            // Arrived at 07:50 although cancelled: it departs at 07:50 + 15 = 08:05, and the next stop
            // chains from there: 08:05 + 25 = 08:30, raised to 08:45.
            var stops = new List<ChainStop>
            {
                PullOut(),
                Pickup(2, 10, H(8, 0), M(30), status: "Canceled", arrive: H(7, 50)),
                Pickup(3, 11, H(9, 0), M(25)),
                PullIn(4, M(15))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(H(8, 45), stops[2].Eta);
        }

        [Fact]
        public void Once_the_vehicle_has_left_only_the_drive_back_moves_the_pull_in()
        {
            // Pull-out performed at 07:05 and the pickup at 07:50: the garage hour is history.
            // D1 = 07:50 + 15 + 20 = 08:25. Pull-in over the new 10-minute drive = 08:40 + 10 = 08:50.
            var stops = new List<ChainStop>
            {
                PullOut(eta: H(7, 10), performed: true, perform: H(7, 5)),
                Pickup(2, 10, H(8, 0), M(30), performed: true, perform: H(7, 50), arrive: H(7, 45)),
                Dropoff(3, 10, H(9, 0), M(20)),
                PullIn(4, M(10))
            };

            RouteEtaChain.Chain(stops);

            Assert.Equal(H(7, 10), stops[0].Eta);
            Assert.Equal(H(8, 25), stops[2].Eta);
            Assert.Equal(H(8, 50), stops[3].Eta);
        }

        [Fact]
        public void A_first_stop_with_no_promised_hour_keeps_the_garage_hour_and_moves_with_the_drive()
        {
            // No Pickup and no Appt: the garage hour is left as it was (07:00), and the first arrival
            // is the drive from it — 07:30 with the old garage, 07:50 with one twenty minutes further.
            var near = new List<ChainStop> { PullOut(), Pickup(2, 10, null, M(30)), PullIn(3, M(15)) };
            var far = new List<ChainStop> { PullOut(), Pickup(2, 10, null, M(50)), PullIn(3, M(15)) };

            RouteEtaChain.Chain(near);
            RouteEtaChain.Chain(far);

            Assert.Equal(H(7, 0), near[0].Eta);
            Assert.Equal(H(7, 30), near[1].Eta);
            Assert.Equal(H(7, 50), far[1].Eta);
        }

        [Fact]
        public void Every_pending_physical_leg_is_priced_from_where_the_vehicle_leaves_at_the_hour_it_leaves()
        {
            var stops = new List<ChainStop>
            {
                PullOut(eta: H(7, 10), performed: true, perform: H(7, 5)),
                Pickup(2, 10, H(8, 0), M(30), performed: true, perform: H(7, 50), arrive: H(7, 45)),
                Pickup(3, 11, H(8, 30), M(10), status: "Canceled"),
                Dropoff(4, 10, H(9, 0), M(20)),
                PullIn(5, M(10))
            };

            RouteEtaChain.Chain(stops);
            var legs = RouteEtaChain.LegsToPrice(stops, Day);

            // The performed pickup and the cancellation nobody reached are not legs.
            Assert.Equal(new[] { 4, 5 }, legs.Select(l => l.To.Id).ToArray());

            // The dropoff is driven from the performed pickup, leaving at 07:50 + 15.
            Assert.Equal(2, legs[0].From.Id);
            Assert.Equal(H(8, 5), legs[0].Departure);
            Assert.Equal(Day, legs[0].Date);

            // And the drive back from the dropoff, leaving at its ETA (08:25) + 15.
            Assert.Equal(4, legs[1].From.Id);
            Assert.Equal(H(8, 40), legs[1].Departure);
        }

        [Fact]
        public void A_leg_with_no_departure_estimate_is_priced_at_the_stop_s_own_hour()
        {
            var stops = new List<ChainStop>
            {
                new() { Id = 1, Name = "Pull-out", Eta = null, Date = Day },
                Pickup(2, 10, H(8, 0), M(30)),
            };

            var legs = RouteEtaChain.LegsToPrice(stops, Day);

            Assert.Single(legs);
            Assert.Equal(H(8, 0), legs[0].Departure);
        }
    }
}
