using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities
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

        //[Required]
        public ScheduleEventType? EventType { get; set; } // Pickup or Dropoff

        //[Required]
        public int? Sequence { get; set; } // To order the events of a route. Ex: 1, 2, 3...

        // --- Pre-calculated data for the grid (denormalization for performance) ---
        [Required]
        [MaxLength(250)]
        public string Name { get; set; } // "Customer Name Pickup - Appointment"

        [MaxLength(200)]
        public string Address { get; set; }
        public double ScheduleLatitude { get; set; }
        public double ScheduleLongitude { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public string? Comment { get; set; }

        [MaxLength(100)]
        public string? FundingSourceName { get; set; }

        [MaxLength(100)]
        public string? AuthNo { get; set; }

        [MaxLength(50)]
        public string? SpaceTypeName { get; set; }

        // --- Times ---
        public TimeSpan? ScheduledPickupTime { get; set; } // Corresponds to the FromTime of the Trip (only for Pickup event)
        public TimeSpan? ScheduledApptTime { get; set; }   // Corresponds to the ToTime of the Trip (only for Dropoff event)

        // --- Fields that will be updated in real time by the driver ---
        public TimeSpan? ETATime { get; set; } // Estimated time of arrival at the point
        public double? DistanceToPoint { get; set; } // Distance to point
        public TimeSpan? TravelTime { get; set; } // Travel time between points

        /// <summary>
        /// How long the driver is planned to sit at a pickup waiting for the hour to come round.
        /// Null on anything that is not a pickup, and on a pickup with no wait.
        /// </summary>
        /// <remarks>
        /// A vehicle is never shown arriving more than a quarter of an hour before the hour the
        /// patient was promised — five minutes on a return — so an ETA that would have been
        /// earlier is raised to the limit. The difference between the hour the driver could have
        /// been there and the hour the route says they arrive is dead time, and until this field
        /// existed it was computed, used to raise the ETA, and thrown away.
        ///
        /// <para>
        /// The dispatcher was left reading a column of arrival hours that all looked reasonable
        /// with no way of telling that one of those drivers was going to stand at a door for two
        /// hours. On a morning with more trips than vehicles that is capacity being burnt in
        /// silence.
        /// </para>
        ///
        /// <para>
        /// ⚠️ Derived, never sent: <c>ScheduleService.ApplyDerivedRouteValuesAsync</c> is the only
        /// writer, and it recomputes this whenever the route's shape changes. A value a client
        /// puts in a request is ignored.
        /// </para>
        /// </remarks>
        public TimeSpan? EarlyArrivalWait { get; set; }

        public TimeSpan? ActualArriveTime { get; set; }
        public TimeSpan? ActualPerformTime { get; set; } // Time in which it is completed (e.g. the passenger gets on or off)

        public double? ArriveDistance { get; set; }
        public double? PerformDistance { get; set; }

        public long? Odometer { get; set; }
        public string? GpsArrive { get; set; } // "lat,lon"
        public DateTime? Date { get; set; }

        public bool Performed { get; set; }

        public byte[]? PassengerSignature { get; set; }
    }
}
