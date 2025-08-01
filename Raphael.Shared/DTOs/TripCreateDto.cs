using System.ComponentModel.DataAnnotations;
using Raphael.Shared.Entities;

namespace Raphael.Shared.DTOs
{
    public class TripCreateDto
    {
        [Required(ErrorMessage = "The day is required.")]
        public string Day { get; set; } = string.Empty;

        [Required(ErrorMessage = "The date is required.")]
        public DateTime Date { get; set; }
      
        public TimeSpan? FromTime { get; set; }

        public TimeSpan? ToTime { get; set; }

        [Required(ErrorMessage = "Customer ID is required.")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Pickup address is required.")]
        public string PickupAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Pickup latitude is required.")]
        public double PickupLatitude { get; set; }

        [Required(ErrorMessage = "Pickup longitude is required.")]
        public double PickupLongitude { get; set; }

        [Required(ErrorMessage = "Dropoff address is required.")]
        public string DropoffAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "Dropoff latitude is required.")]
        public double DropoffLatitude { get; set; }

        [Required(ErrorMessage = "Dropoff longitude is required.")]
        public double DropoffLongitude { get; set; }

        [Required(ErrorMessage = "Space type ID is required.")]
        public int SpaceTypeId { get; set; }

        public double? Charge { get; set; }
        public double? Paid { get; set; }

        [Required(ErrorMessage = "Trip type is required.")]
        //[RegularExpression($"^({TripType.Appointment}|{TripType.Return})$",
            //ErrorMessage = "Invalid trip type. Valid values: 'Appointment' or 'Return'.")]
        public string Type { get; set; } = TripType.Appointment;

        public string? Pickup { get; set; }
        public string? PickupPhone { get; set; }
        public string? PickupComment { get; set; }

        // Corrected property names
        public string? Dropoff { get; set; }
        public string? DropoffPhone { get; set; }
        public string? DropoffComment { get; set; }

        public string? TripId { get; set; }  // External system identifier
        public string? Authorization { get; set; }
        public double? Distance { get; set; }  // In miles
        public double? ETA { get; set; }  // In minutes

        [Required(ErrorMessage = "WillCall status is required.")]
        public bool WillCall { get; set; }
        public string Status { get; set; }
        public int? VehicleRouteId { get; set; }
        public string? DriverNoShowReason { get; set; }
        public int? FundingSourceId { get; set; }
    }
}
