namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// What the Booking Portal's tracking view shows for one trip: where, when it is expected, and
    /// what has already happened.
    /// </summary>
    /// <remarks>
    /// Times are the business's wall-clock hours of the trip's day, as the route computes them
    /// (TIME_POLICY): shown as they are, never converted. The ETAs are the route's own
    /// (<c>Schedule.ETATime</c>), so they cost nothing to read; they are null until the trip is routed.
    /// </remarks>
    public class TripTrackingDto
    {
        public int TripId { get; set; }

        public string Status { get; set; } = string.Empty;

        public bool IsCancelled { get; set; }

        /// <summary>True while the vehicle may be shown: driver on the way, at the door, or carrying the patient.</summary>
        public bool InProgress { get; set; }

        public DateTime Date { get; set; }

        public string PickupAddress { get; set; } = string.Empty;

        public double PickupLatitude { get; set; }

        public double PickupLongitude { get; set; }

        public string DropoffAddress { get; set; } = string.Empty;

        public double DropoffLatitude { get; set; }

        public double DropoffLongitude { get; set; }

        /// <summary>The pickup time the clinic booked.</summary>
        public TimeSpan? RequestedPickupTime { get; set; }

        /// <summary>The appointment time the clinic booked.</summary>
        public TimeSpan? AppointmentTime { get; set; }

        public TimeSpan? PickupEta { get; set; }

        public TimeSpan? DropoffEta { get; set; }

        /// <summary>When the driver reached the pickup.</summary>
        public TimeSpan? PickupArrivedAt { get; set; }

        /// <summary>When the patient got on board.</summary>
        public TimeSpan? PickedUpAt { get; set; }

        public TimeSpan? DropoffArrivedAt { get; set; }

        /// <summary>When the patient was left at the destination.</summary>
        public TimeSpan? DroppedOffAt { get; set; }

        /// <summary>The provider carrying the trip. Null means Raphael's own fleet.</summary>
        public string? ProviderName { get; set; }

        /// <summary>
        /// The kind of vehicle on the route ("Wheelchair", "Stretcher"), or null before routing.
        /// </summary>
        /// <remarks>
        /// ⚠️ This, and the provider, is all a clinic learns of the vehicle. The route's and the
        /// vehicle's own names carry the driver's full name and fleet codes (seen in DEV on
        /// 2026-10-10), so they never leave the office. Decision of 2026-10-10.
        /// </remarks>
        public string? VehicleType { get; set; }
    }
}
