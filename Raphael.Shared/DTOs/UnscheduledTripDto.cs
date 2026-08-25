namespace Raphael.Shared.DTOs
{
    public class UnscheduledTripDto
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string CustomerName { get; set; }

        public string? CustomerPhone { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        public string PickupAddress { get; set; }
        public string DropoffAddress { get; set; }
        public string SpaceType { get; set; }
        public string FundingSource { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public double? Distance { get; set; }
        public double? Charge { get; set; }
        public double? Paid { get; set; }
        public string? Type { get; set; } // (Appointment, Return)
        public string? Pickup { get; set; }
        public string? PickupPhone { get; set; }
        public string? PickupComment { get; set; }
        public string? Dropoff { get; set; }
        public string? DropoffPhone { get; set; }
        public string? DropoffComment { get; set; }
        public string? TripId { get; set; } // Funding Sources / Brokers Identifier
        public string? Authorization { get; set; }
        public bool WillCall { get; set; }
        public string Status { get; set; }
        public int? FundingSourceId { get; set; }
        public string? DriverNoShowReason { get; set; }
        public string? PickupCity { get; set; }
        public string? DropoffCity { get; set; }
        public bool IsCanceled { get; set; }

        /// <summary>Provider operating the trip. Null means the broker runs it itself.</summary>
        public int? ProviderId { get; set; }

        /// <summary>
        /// The timezone this trip is operated in, as an IANA identifier.
        /// </summary>
        /// <remarks>
        /// ⚠️ Already resolved through the provider's fallback chain, so it is never null and
        /// never the hosting machine's zone. Desktop needs it to suggest "now" when a
        /// dispatcher activates a Will Call: the hour that matters is the one at the pickup
        /// address, not the one on the dispatcher's PC. See <c>_meta/TIME_POLICY.md</c>.
        /// </remarks>
        public string? ProviderTimeZoneId { get; set; }
    }
}

