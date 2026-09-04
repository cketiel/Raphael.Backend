using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// One row of a broker's CSV file, already mapped by the desktop app.
    /// </summary>
    /// <remarks>
    /// Everything the server needs to store the trip travels in this object, including the
    /// patient and the space type, so that a file of four hundred trips costs a handful of
    /// requests instead of one per row per lookup.
    ///
    /// <para>
    /// The shared host reads a burst of requests from one address as an attack and withdraws
    /// the permissions of the whole application, which is how an import used to end with the
    /// connection dropped and part of the file missing. Batching is not an optimisation here:
    /// it is what keeps the import finishing at all.
    /// </para>
    ///
    /// <para>
    /// Names and identifiers rather than foreign keys, deliberately. The desktop app cannot
    /// know the id of a patient it has not created yet, and having it ask would put the
    /// round trip back that this endpoint exists to remove.
    /// </para>
    /// </remarks>
    public class TripImportItemDto
    {
        /// <summary>The broker's own identifier for the trip. This is what an import is matched on.</summary>
        [Required(ErrorMessage = "TripId is required.")]
        public string TripId { get; set; } = string.Empty;

        /// <summary>Calendar day of the trip. Any time of day is ignored.</summary>
        [Required(ErrorMessage = "The date is required.")]
        public DateTime Date { get; set; }

        /// <summary>Pickup time, or null when the file leaves it open.</summary>
        public TimeSpan? FromTime { get; set; }

        /// <summary>Appointment time, or null.</summary>
        public TimeSpan? ToTime { get; set; }

        // --- Journey ---

        [Required(ErrorMessage = "Pickup address is required.")]
        public string PickupAddress { get; set; } = string.Empty;

        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }

        [Required(ErrorMessage = "Dropoff address is required.")]
        public string DropoffAddress { get; set; } = string.Empty;

        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }

        public string? PickupCity { get; set; }
        public string? DropoffCity { get; set; }

        /// <summary>Distance in miles, as the broker states it.</summary>
        public double? Distance { get; set; }

        /// <summary>Appointment or Return. Decided by the client across the whole file.</summary>
        public string? Type { get; set; }

        /// <summary>
        /// True when the patient rings to be collected instead of holding a booked hour.
        /// </summary>
        /// <remarks>
        /// ⚠️ Written on creation only. On an existing trip this field carries an hour promised
        /// to a patient and moves solely through Activate / Back to Will Call, which record who
        /// did it and tell the patient. Re-importing a file must not flip it silently.
        /// </remarks>
        public bool WillCall { get; set; }

        public string? Pickup { get; set; }
        public string? Dropoff { get; set; }
        public string? PickupPhone { get; set; }
        public string? DropoffPhone { get; set; }
        public string? PickupComment { get; set; }
        public string? DropoffComment { get; set; }

        // --- Space type, resolved by name ---

        /// <summary>Short code of the space type: AMB, WCH, STR and the rest.</summary>
        [Required(ErrorMessage = "SpaceTypeName is required.")]
        public string SpaceTypeName { get; set; } = string.Empty;

        /// <summary>Used only when the space type has to be created on first sight.</summary>
        public string? SpaceTypeDescription { get; set; }

        /// <summary>Capacity the space type counts against, used only when creating it.</summary>
        public string? CapacityTypeName { get; set; }

        // --- Patient, resolved by RiderId and then by name and phone ---

        /// <summary>The key the patient is matched on. Built by the client when the file has none.</summary>
        public string? RiderId { get; set; }

        [Required(ErrorMessage = "CustomerFullName is required.")]
        public string CustomerFullName { get; set; } = string.Empty;

        public string? CustomerPhone { get; set; }
        public string? CustomerMobilePhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerState { get; set; }
        public string? CustomerZip { get; set; }
        public string? CustomerGender { get; set; }
        public DateTime? CustomerDOB { get; set; }
    }

    /// <summary>
    /// A chunk of a CSV import: the funding source it was filed under, and its rows.
    /// </summary>
    /// <remarks>
    /// The client splits a file into chunks and sends them one after another, never at the
    /// same time. The chunk exists so a request stays inside its timeout and so progress can
    /// be reported, not because of any size limit: a hundred rows measured 171 KB.
    /// </remarks>
    public class TripImportRequestDto
    {
        /// <summary>The funding source chosen on the import screen. It applies to every row.</summary>
        [Required(ErrorMessage = "FundingSourceId is required.")]
        public int FundingSourceId { get; set; }

        public List<TripImportItemDto> Items { get; set; } = new();
    }
}
