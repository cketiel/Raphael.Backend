using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class IntegrationTripDto
    {
        [Required]
        public string TripId { get; set; } 
        [Required]
        public DateTime Date { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }

        // Customer Data
        public string? RiderId { get; set; }
        [Required]
        public string CustomerFullName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerZip { get; set; }
        public string? CustomerGender { get; set; } = "Unknown";

        // References by Name
        /// <summary>
        /// Code representing the vehicle/space type required.
        /// </summary>
        /// <remarks>
        /// Allowed values:
        /// - **AMB**: Ambulatory (Patient can walk).
        /// - **WCH**: WheelChair (Patient requires a wheelchair).
        /// - **STR**: Stretcher (Patient requires a stretcher).
        /// </remarks>
        [Required]
        [RegularExpression("AMB|WCH|STR", ErrorMessage = "SpaceTypeName must be AMB, WCH, or STR")]
        public string SpaceTypeName { get; set; }
        [Required]
        public string FundingSourceName { get; set; }

        // Trip Information
        [Required]
        public string PickupAddress { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        [Required]
        public string DropoffAddress { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public double Distance { get; set; }  // In miles
        public string PickupCity { get; set; }
        public string DropoffCity { get; set; }

        /// <summary>
        /// Type of the trip. This field is optional.
        /// </summary>
        /// <remarks>
        /// If provided, it must be exactly one of the following:
        /// - **Appointment**: Outgoing trip.
        /// - **Return**: Returning trip.
        /// If omitted, the system defaults to 'Appointment'.
        /// </remarks>
        [RegularExpression("Appointment|Return", ErrorMessage = "Type must be either 'Appointment' or 'Return'")]
        public string? Type { get; set; }
        public string? Authorization { get; set; }

        public string? PickupComment { get; set; }
        public string? DropoffComment { get; set; }
        public string? NotificationEmail { get; set; }
        public IFormFile? Attachment { get; set; }

        public string? Status { get; set; }
    }
}
