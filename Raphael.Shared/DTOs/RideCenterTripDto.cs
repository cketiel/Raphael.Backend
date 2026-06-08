using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class RideCenterTripDto
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
        [Required]
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
        public string? Type { get; set; } // Appointment / Return
        public string? Authorization { get; set; }
    }
}
