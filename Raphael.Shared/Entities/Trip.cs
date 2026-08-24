using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Raphael.Shared.Entities
{
    /// <summary>
    /// Kind of leg within a patient's journey.
    /// </summary>
    public static class TripType
    {
        /// <summary>Outbound leg: from home to the medical appointment.</summary>
        public static string Appointment = "Appointment";

        /// <summary>Return leg: from the facility back home.</summary>
        public static string Return = "Return";
    }

    /// <summary>
    /// Life cycle of a trip. Every change is recorded in the TripLogs table.
    /// </summary>
    /// <remarks>
    /// Stored as text in <see cref="Trip.Status"/>, so adding a value needs no migration.
    /// It does change a contract: Raphael.Desktop mirrors these constants, Raphael.Rider
    /// mirrors them as a TypeScript enum with a colour each, and integrations receive the
    /// string as is. A value they do not know renders blank or is misclassified.
    ///
    /// <para>
    /// Normal order of a trip that goes well:
    /// Assigned → Accepted → Scheduled → Started → Arrived → InProgress → Finished,
    /// and later Billed and Payed. <see cref="Waiting"/> and <see cref="Canceled"/> come
    /// off that path.
    /// </para>
    /// </remarks>
    public static class TripStatus
    {
        /// <summary>The Broker/Funding Source assigns the trip. The Router is notified.</summary>
        public static string Assigned = "Assigned";

        /// <summary>The Supplier accepts the trip. The Broker is notified. (Member may be notified.)</summary>
        public static string Accepted = "Accepted";

        /// <summary>The Router schedules the trip, designates a Driver and a Vehicle to carry out the trip. The Driver is notified.</summary>
        public static string Scheduled = "Scheduled";

        /// <summary>The Driver selects the trip and heads to the pick-up address. The Member waits and is notified that the Driver is on its way.</summary>
        public static string Started = "Started";

        /// <summary>
        /// The Driver has reached the pick-up address and is waiting for the Member to
        /// board. The Member is notified that the vehicle is already there.
        /// </summary>
        /// <remarks>
        /// Sits between <see cref="Started"/> and <see cref="InProgress"/>. It is written
        /// when the driver presses Arrive on the pickup event, not when the pickup is
        /// performed: those are two different moments and a dispatcher needs to tell them
        /// apart to know whether the vehicle is en route or already at the door.
        /// </remarks>
        public static string Arrived = "Arrived";

        /// <summary>
        /// The Member reported being ready on a trip booked as Will Call, with no pick-up
        /// time, and is waiting for a vehicle to be dispatched. From this moment the
        /// office has one hour to get one there.
        /// </summary>
        /// <remarks>
        /// This is not "the driver is waiting": that is <see cref="Arrived"/>. Here nobody
        /// is on the way yet, which is exactly why the dispatcher has to act.
        /// </remarks>
        public static string Waiting = "Waiting";

        /// <summary>The Driver is late to the pickup address with respect to the Pickup Time or is late with respect to the Appointment Time. Dispatcher is alerted. The Driver is notified.</summary>
        public static string Late = "Late";

        /// <summary>The Driver selects to start the trip and heads from the pick-up address to the drop-off address location. Dispatcher is notified.</summary>
        public static string InProgress = "InProgress";

        /// <summary>The Driver selects to end the trip. The Driver finishes the trip, leaving the Member at their destination. Dispatcher is notified. The Broker is notified.</summary>
        public static string Finished = "Finished";

        /// <summary>The trip was cancelled. All those involved in the process are alerted: the Provider, the Router, the Dispatcher, the Driver.</summary>
        public static string Canceled = "Canceled";

        /// <summary>Is when the FUNDING SOURCE was invoiced.</summary>
        public static string Billed = "Billed";

        /// <summary>Is when the FUNDING SOURCE paid.</summary>
        public static string Payed = "Payed";
    }

    /// <summary>
    /// A patient's journey between two addresses on a given date. Core entity of the
    /// system: everything else (routes, schedules, billing, notifications) hangs off it.
    /// </summary>
    /// <remarks>
    /// Contains PHI: patient, addresses and phone numbers. None of it belongs in logs,
    /// query strings or notification texts.
    /// </remarks>
    public class Trip
    {
        /// <summary>Internal identifier of the trip.</summary>
        public int Id { get; set; }

        /// <summary>Day of the week the trip takes place on.</summary>
        /// <example>Monday</example>
        [Required]
        public string Day { get; set; } = string.Empty;

        /// <summary>Date of the trip.</summary>
        [Required]
        public DateTime Date { get; set; }

        /// <summary>
        /// Start of the pick-up window. Null on a Will Call trip until the patient
        /// reports being ready, when it is set to that moment.
        /// </summary>
        public TimeSpan? FromTime { get; set; }

        /// <summary>End of the pick-up window.</summary>
        public TimeSpan? ToTime { get; set; }

        /// <summary>Patient being transported.</summary>
        [Required]
        public int CustomerId { get; set; }

        [ForeignKey("CustomerId")]
        public virtual Customer Customer { get; set; }
        //public Customer Customer { get; set; } = new Customer();

        /// <summary>Pick-up address. PHI.</summary>
        [Required]
        public string PickupAddress { get; set; } = string.Empty;

        /// <summary>Latitude of the pick-up address.</summary>
        [Required]
        public double PickupLatitude { get; set; }

        /// <summary>Longitude of the pick-up address.</summary>
        [Required]
        public double PickupLongitude { get; set; }

        /// <summary>Drop-off address. PHI.</summary>
        [Required]
        public string DropoffAddress { get; set; } = string.Empty;

        /// <summary>Latitude of the drop-off address.</summary>
        [Required]
        public double DropoffLatitude { get; set; }

        /// <summary>Longitude of the drop-off address.</summary>
        [Required]
        public double DropoffLongitude { get; set; }

        /// <summary>Space the patient needs: ambulatory, wheelchair, stretcher.</summary>
        [Required]
        public int SpaceTypeId { get; set; }

        [ForeignKey("SpaceTypeId")]
        public virtual SpaceType SpaceType { get; set; }
        //public SpaceType SpaceType { get; set; } = new SpaceType();

        /// <summary>
        /// Cancellation flag, kept alongside <see cref="Status"/> because the unique
        /// index of active trips filters on it.
        /// </summary>
        public bool IsCancelled { get; set; }

        // new

        /// <summary>Amount charged for the trip.</summary>
        public double? Charge { get; set; }

        /// <summary>Amount actually collected.</summary>
        public double? Paid { get; set; }

        /// <summary>Leg of the journey. See <see cref="TripType"/>.</summary>
        /// <example>Appointment</example>
        [Required]
        public string Type { get; set; } = TripType.Appointment; // (Appointment, Return)

        /// <summary>Name of the pick-up place.</summary>
        public string? Pickup { get; set; }

        /// <summary>Phone number at the pick-up place. PHI.</summary>
        public string? PickupPhone { get; set; }

        /// <summary>Instructions for the driver at pick-up.</summary>
        public string? PickupComment { get; set; }

        /// <summary>Name of the drop-off place.</summary>
        public string? Dropoff { get; set; }

        /// <summary>Phone number at the drop-off place. PHI.</summary>
        public string? DropoffPhone { get; set; }

        /// <summary>Instructions for the driver at drop-off.</summary>
        public string? DropoffComment { get; set; }

        /// <summary>Identifier of the trip in the Funding Source or Broker system.</summary>
        public string? TripId { get; set; } // Funding Sources / Brokers Identifier

        /// <summary>Authorization number covering the trip.</summary>
        public string? Authorization { get; set; }

        /// <summary>Distance in miles.</summary>
        public double? Distance { get; set; } // Distance in miles, then make unit of measurement converters class.

        /// <summary>Estimated time in minutes.</summary>
        public double? ETA { get; set; } // ETA in minutes, then do a class converting units of time to decimal and vice versa.

        /// <summary>Route the trip is assigned to. Null while it is still unassigned.</summary>
        public int? VehicleRouteId { get; set; }

        [ForeignKey("VehicleRouteId")]
        public virtual VehicleRoute Run { get; set; }
        //public VehicleRoute? Run { get; set; }

        /// <summary>
        /// True while the pick-up time is unknown and the patient must report being
        /// ready. Set back to false the moment they do.
        /// </summary>
        [Required]
        public bool WillCall { get; set; } = false;

        /// <summary>Current state of the trip. See <see cref="TripStatus"/>.</summary>
        /// <example>Scheduled</example>
        [Required]
        public string Status { get; set; } = TripStatus.Assigned;

        /// <summary>Reason given by the driver when cancelling on site (no show).</summary>
        public string? DriverNoShowReason { get; set; }

        /// <summary>When the trip record was created.</summary>
        [Required]
        public DateTime Created { get; set; }

        /// <summary>
        /// Funding Source that covers the trip, kept for history: the patient may change
        /// theirs later, and a trip may be paid directly with none at all.
        /// </summary>
        public int? FundingSourceId { get; set; } // You have to save the Funding Source for the history. Because the Customer can change the Funding Source and the history is lost. Also to allow the Customer to not be required to have Funding Source and can make payments directly.

        [ForeignKey("FundingSourceId")]
        public virtual FundingSource FundingSource { get; set; }
       // public FundingSource FundingSource { get; set; } = new FundingSource();

        /// <summary>History of state changes.</summary>
        public ICollection<TripLog> TripLogs { get; set; }

        /// <summary>City of the pick-up location.</summary>
        public string? PickupCity { get; set; } // The city for the pickup location

        /// <summary>City of the drop-off location.</summary>
        public string? DropoffCity { get; set; } // The city for the dropoff location

        /// <summary>
        /// External system that created the trip. Null means it was created by the
        /// Broker, that is, from Raphael.Desktop.
        /// </summary>
        public int? IntegratorId { get; set; }

        [ForeignKey("IntegratorId")]
        public virtual Integrator? Integrator { get; set; }

        /// <summary>Provider carrying out the trip, when it is subcontracted.</summary>
        public int? ProviderId { get; set; }

        [ForeignKey("ProviderId")]
        public virtual Provider? Provider { get; set; }

    }
}
