using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Metrics;

namespace Meditrans.Shared.Entities
{   
    public class Trip
    {
        public int Id { get; set; }
        [Required]
        public string Day { get; set; }
        [Required]
        public DateTime Date { get; set; }
        [Required]
        public TimeSpan FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }
        [Required]
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        [Required]
        public string PickupAddress { get; set; }
        [Required]
        public double PickupLatitude { get; set; }
        [Required]
        public double PickupLongitude { get; set; }
        [Required]
        public string DropoffAddress { get; set; }
        [Required]
        public double DropoffLatitude { get; set; }
        [Required]
        public double DropoffLongitude { get; set; }
        [Required]
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }

        //public string? PickupNote { get; set; } // set to PickupComment
        public bool IsCancelled { get; set; }

        // new
        public double? Charge { get; set; }
        public double? Paid { get; set; }
        public string? Type { get; set; } // (Appointment, Return)
        public string? Pickup { get; set; }
        public string? PickupPhone { get; set; }
        public string? PickupComment { get; set; }
        public string? Dropoof { get; set; }
        public string? DropoofPhone { get; set; }
        public string? DropoofComment { get; set; }
        public string? TripId { get; set; } // Funding Sources / Brokers Identifier
        public double? Distance { get; set; } // Distance in miles, then make unit of measurement converters class.
        public double? ETA { get; set; } // ETA in minutes, then do a class converting units of time to decimal and vice versa.
        public int VehicleRouteId { get; set; }
        public VehicleRoute Run { get; set; }
        public bool WillCall { get; set; }
        public string? DriverNoShowReason { get; set; }
        [Required]
        public DateTime Created { get; set; }

        //public ICollection<Schedule> Schedules { get; set; }
    }
}
