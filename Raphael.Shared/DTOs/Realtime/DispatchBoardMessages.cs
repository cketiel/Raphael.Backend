using System;
using Raphael.Shared.DTOs.CallRequests;

namespace Raphael.Shared.DTOs.Realtime
{
    // ===== Messages of the dispatch board channel =====
    //
    // ⚠️ These are NOT notifications. Nothing here is stored, nothing reaches anybody's inbox,
    // nothing counts as unread and nothing has a retention window — there is no purge to
    // configure because there is nothing to purge. The inbox is for people; this is for screens.
    // A dispatcher who was not looking has lost nothing: opening the tab loads the state.
    //
    // ⚠️ Every message carries identifiers and never patient data. A screen that receives one
    // turns the id into data through the ordinary authorised endpoints, which already apply the
    // provider filter. So even a message delivered to the wrong group shows nobody a name, an
    // address or a telephone number.

    /// <summary>
    /// A trip was put on a route. Whoever is looking at the backlog should stop offering it.
    /// </summary>
    public class TripRoutedMessage
    {
        public int TripId { get; set; }

        public int VehicleRouteId { get; set; }

        public DateTime Date { get; set; }
    }

    /// <summary>
    /// A trip was taken off its route and is waiting again.
    /// </summary>
    public class TripUnroutedMessage
    {
        public int TripId { get; set; }

        public int VehicleRouteId { get; set; }

        public DateTime Date { get; set; }
    }

    /// <summary>
    /// The stops of one route on one day moved — reordered, or their hours recalculated.
    /// </summary>
    /// <remarks>
    /// Deliberately says only which route changed, not how. The receiving screen reloads that
    /// one route, which is a single query, instead of the sender trying to describe a
    /// rearrangement that the receiver may be showing under a different filter anyway.
    /// </remarks>
    public class RouteChangedMessage
    {
        public int VehicleRouteId { get; set; }

        public DateTime Date { get; set; }
    }

    /// <summary>
    /// Where a vehicle is, as its driver last reported.
    /// </summary>
    /// <remarks>
    /// <see cref="AtUtc"/> is the instant the fix was taken, and it is what lets the screen
    /// animate the vehicle over the real gap between two reports instead of guessing one.
    /// </remarks>
    public class VehiclePositionMessage
    {
        public int VehicleRouteId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Speed { get; set; }

        /// <summary>
        /// The heading as the driver's app reports it — a string, matching GpsDataDto, because
        /// that is what the screens already know how to turn into an angle.
        /// </summary>
        public string? Direction { get; set; }

        public DateTime AtUtc { get; set; }
    }

    /// <summary>
    /// Where the vehicle carrying one of a clinic's trips is, sent only while that trip is under
    /// way (driver on the way, at the door, or carrying the patient).
    /// </summary>
    /// <remarks>
    /// Keyed by trip, not by route: the clinic never learns the route, and outside that window
    /// the vehicle is on its way to other patients, whose homes its position would give away.
    /// </remarks>
    public class TripVehiclePositionMessage
    {
        public int TripId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public double Speed { get; set; }

        public string? Direction { get; set; }

        public DateTime AtUtc { get; set; }

        /// <summary>
        /// Where the vehicle is heading for this trip: <see cref="TripPhase.Pickup"/> until the
        /// patient is on board, <see cref="TripPhase.Dropoff"/> after.
        /// </summary>
        public string Phase { get; set; } = TripPhase.Pickup;

        /// <summary>
        /// Miles still to go to the stop of <see cref="Phase"/>, measured at home on every fix,
        /// never asked of Google (RemainingDistance). Null when it could not be measured.
        /// </summary>
        public double? RemainingMiles { get; set; }

        /// <summary>
        /// The route's current ETAs for the two stops, as the driver's app last left them. They
        /// travel with every fix so a new ETA reaches the screen with the next position, with no
        /// event of its own. Wall-clock hours of the trip's day (TIME_POLICY).
        /// </summary>
        public TimeSpan? PickupEta { get; set; }

        public TimeSpan? DropoffEta { get; set; }
    }

    /// <summary>The two legs of a trip a vehicle can be on.</summary>
    public static class TripPhase
    {
        public const string Pickup = "Pickup";

        public const string Dropoff = "Dropoff";
    }

    /// <summary>
    /// One of a clinic's trips changed status. The Booking Portal updates that row in place.
    /// </summary>
    /// <remarks>
    /// The portal's own message, not a notification: it carries every status change (Arrived and
    /// InProgress too), and the integrators that use the API never see it, so their contract is
    /// untouched. Nothing here is stored.
    /// </remarks>
    public class TripStatusChangedMessage
    {
        public int TripId { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsCancelled { get; set; }

        public DateTime Date { get; set; }
    }

    /// <summary>What a clinic gets back when it asks to follow one of its trips.</summary>
    public class WatchTripResult
    {
        /// <summary>
        /// True while the trip is under way. When false the screen says that the vehicle can only
        /// be seen during the trip, and no position is sent.
        /// </summary>
        public bool InProgress { get; set; }

        /// <summary>The last position reported, so the map does not wait for the next fix. Null when none.</summary>
        public TripVehiclePositionMessage? Position { get; set; }
    }

    /// <summary>
    /// A driver's call request changed: it arrived, was reminded, taken, released or closed.
    /// </summary>
    /// <remarks>
    /// Carries the driver's and the route's names, which are staff data, and no patient data.
    /// <see cref="Change"/> is one of the timeline types (Requested, Reminded, Claimed, TakenOver,
    /// Released, CallNotAnswered, DriverAvailable, Resolved, Cancelled, Reopened, Expired).
    /// </remarks>
    public class CallRequestChangedMessage
    {
        public string Change { get; set; } = string.Empty;

        /// <summary>Who caused it, so their own screen does not announce it back to them. Null for the system.</summary>
        public int? ByUserId { get; set; }

        public CallRequestSummaryDto Request { get; set; } = new();
    }
}
