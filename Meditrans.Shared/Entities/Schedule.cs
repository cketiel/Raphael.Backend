using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Meditrans.Shared.Entities
{
    // Denormalized Fields(Name, Address, FundingSourceName, etc.): We store this data directly in the Schedule table.
    // This makes the query to populate the top grid extremely fast and simple,
    // as it does not require multiple JOINs every time the view is refreshed.
    // The cost is a little more disk space, but the performance benefit is huge for such an interactive display.
    public class Schedule
    {
        public int Id { get; set; }

        // TripId is now nullable to allow non-trip schedules (Pull-out/Pull-in)
        public int? TripId { get; set; }

        [ForeignKey("TripId")]
        public Trip Trip { get; set; }

        [Required]
        public int VehicleRouteId { get; set; }
        [ForeignKey("VehicleRouteId")]
        public VehicleRoute VehicleRoute { get; set; }

        [Required]
        public ScheduleEventType EventType { get; set; } // Pickup or Dropoff

        [Required]
        public int Sequence { get; set; } // To order the events of a route. Ex: 1, 2, 3...

        // --- Pre-calculated data for the grid (denormalization for performance) ---
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } // "Customer Name Pickup - Appointment"

        [MaxLength(200)]
        public string Address { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? Comment { get; set; }

        [MaxLength(100)]
        public string FundingSourceName { get; set; }

        [MaxLength(100)]
        public string? AuthNo { get; set; }

        [MaxLength(50)]
        public string SpaceTypeName { get; set; }

        // --- Times ---
        public TimeSpan? ScheduledPickupTime { get; set; } // Corresponds to the FromTime of the Trip (only for Pickup event)
        public TimeSpan? ScheduledApptTime { get; set; }   // Corresponds to the ToTime of the Trip (only for Dropoff event)

        // --- Fields that will be updated in real time by the driver ---
        public TimeSpan? ETATime { get; set; } // Estimated time of arrival at the point
        public double? DistanceToPoint { get; set; } // Distance to point
        public TimeSpan? TravelTime { get; set; } // Travel time between points

        public TimeSpan? ActualArriveTime { get; set; }
        public TimeSpan? ActualPerformTime { get; set; } // Time in which it is completed (e.g. the passenger gets on or off)

        public double? ArriveDistance { get; set; }
        public double? PerformDistance { get; set; }

        public long? Odometer { get; set; }
        public string? GpsArrive { get; set; } // "lat,lon"
    }
}