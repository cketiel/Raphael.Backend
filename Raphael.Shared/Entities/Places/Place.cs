using System.ComponentModel.DataAnnotations;
using Raphael.Shared.Interfaces;

namespace Raphael.Shared.Entities.Places
{
    /// <summary>What kind of place a <see cref="Place"/> is.</summary>
    public enum PlaceKind
    {
        /// <summary>A patient's home. Carries <see cref="Place.CustomerId"/>.</summary>
        Home = 1,

        /// <summary>A hospital, clinic, dialysis centre, nursing home… shared by every patient that goes there.</summary>
        Facility = 2,
    }

    /// <summary>
    /// A place trips go to or come from, with the notes that guide the driver there (RE-054).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A place is an address, not a point: a hospital is one place with several
    /// <see cref="PlaceEntrance"/>s (emergency, outpatients…), and the trip keeps its own
    /// coordinates because the driver works with those.
    /// </para>
    /// <para>
    /// The identity is ours, not Google's: places are found by <see cref="AddressKey"/> and by the
    /// coordinates the trip already carries, so finding one never calls Google (MAPS_POLICY).
    /// </para>
    /// <para>
    /// A home's notes are PHI (they describe where a patient lives). None of it goes to logs.
    /// </para>
    /// </remarks>
    public class Place : IOwnedByProvider
    {
        public int Id { get; set; }

        public PlaceKind Kind { get; set; }

        /// <summary>"Jackson Memorial Hospital", or "&lt;patient&gt; Home". Editable by the office.</summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>The address as first seen. PHI on a home.</summary>
        [Required]
        [MaxLength(450)]
        public string Address { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? City { get; set; }

        /// <summary>
        /// <see cref="Address"/> normalised by <c>PlaceAddressKey</c>: the lookup column. Never shown.
        /// </summary>
        [Required]
        [MaxLength(MaxAddressKeyLength)]
        public string AddressKey { get; set; } = string.Empty;

        /// <summary>400, not 450: it shares a unique index with two ints, inside SQL Server's 900-byte limit.</summary>
        public const int MaxAddressKeyLength = 400;

        /// <summary>The patient whose home this is. Null on a facility.</summary>
        public int? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// The catalog entry this facility is, once the office links it. Empty for now: facilities may
        /// become a catalog category later, and this keeps that a link and not a data move.
        /// </summary>
        public int? CatalogIntegratorId { get; set; }

        /// <summary>Where the row came from (import, office, driver, history seed).</summary>
        public NoteSource Source { get; set; }

        public bool IsActive { get; set; } = true;

        /// <inheritdoc />
        public int? OwnerProviderId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }

        public ICollection<PlaceEntrance> Entrances { get; set; } = new List<PlaceEntrance>();
        public ICollection<PlaceNote> Notes { get; set; } = new List<PlaceNote>();
    }
}
