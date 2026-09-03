using System;

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
}
