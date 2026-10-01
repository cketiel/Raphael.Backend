using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>
    /// An entity that could send us trips — a nursing home, an assisted living residence, a
    /// hospital — whether or not we work with it yet.
    /// </summary>
    /// <remarks>
    /// This is not an <see cref="Integrator"/>. An Integrator is somebody we already work with and
    /// who has a key against our API. This is the directory we work the phone from: who they are,
    /// where they are, who to ask for. A row is never deleted — when we stop being interested it
    /// goes <see cref="IsActive"/> false, because the reason we looked at it once is worth keeping.
    ///
    /// <para>
    /// When an arrangement is reached, the row is added to our company: an <see cref="Integrator"/>
    /// is created pointing back here through <c>Integrator.CatalogIntegratorId</c>, and this row
    /// stays exactly where it is. Several providers can add the same entity, each one getting its
    /// own Integrator row.
    /// </para>
    /// </remarks>
    public class CatalogIntegrator
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public CatalogIntegratorCategory Category { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        // ---- Where it is -------------------------------------------------------------------

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        public int? CountyId { get; set; }
        public County? County { get; set; }

        /// <summary>
        /// The county exactly as the source file spelled it, kept even when it matched
        /// <see cref="CountyId"/>. When it did not match, this is the only trace of what the
        /// file actually said, and that is what tells us the matching needs fixing.
        /// </summary>
        [MaxLength(100)]
        public string? CountyRaw { get; set; }

        [MaxLength(2)]
        public string? State { get; set; }

        [MaxLength(20)]
        public string? Zip { get; set; }

        // ---- How to reach it ---------------------------------------------------------------

        /// <summary>The phone as it was written down, in whatever shape the source used.</summary>
        [MaxLength(40)]
        public string? Phone { get; set; }

        /// <summary>
        /// <see cref="Phone"/> with everything that is not a digit removed, filled by the service
        /// on save. The source files write the same number three ways — (407) 786-5637,
        /// 352-222-9600, 4078138000 — and a dispatcher types a fourth. Searching this column is
        /// what makes all of them find the row.
        /// </summary>
        [MaxLength(20)]
        public string? PhoneDigits { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Website { get; set; }

        /// <summary>Administrator, executive director, whoever the source named.</summary>
        [MaxLength(200)]
        public string? ContactName { get; set; }

        // ---- Particular to this kind of entity ----------------------------------------------

        /// <summary>"Assisted living facility (ALF)", "PART A PROVIDER - HOSPITAL", and the like.</summary>
        [MaxLength(150)]
        public string? FacilityType { get; set; }

        /// <summary>Certified or licensed beds. A rough measure of how much work they could send.</summary>
        public int? Beds { get; set; }

        /// <summary>The group it belongs to, when it belongs to one.</summary>
        [MaxLength(200)]
        public string? ChainName { get; set; }

        // ---- Working the contact ------------------------------------------------------------

        /// <summary>Free notes on the conversations. Not a log: whatever is worth remembering.</summary>
        public string? Comments { get; set; }

        public bool IsActive { get; set; } = true;

        // ---- Coordinates ---------------------------------------------------------------------

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public GeocodeStatus GeocodeStatus { get; set; } = GeocodeStatus.NotRequested;

        public DateTime? GeocodedAtUtc { get; set; }

        // ---- Where the row came from -----------------------------------------------------------

        /// <summary>
        /// The natural key: category, name and ZIP, upper-cased and stripped of punctuation.
        /// Unique. This is what makes re-importing the same file update the rows instead of
        /// doubling the catalog.
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string MatchKey { get; set; }

        [MaxLength(260)]
        public string? SourceFile { get; set; }

        /// <summary>Row number in that file, so a bad row can be found in the original.</summary>
        public int? SourceRow { get; set; }

        public Guid? ImportBatchId { get; set; }

        // ---- Who touched it --------------------------------------------------------------------

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public int? CreatedByUserId { get; set; }

        /// <summary>
        /// The company whose user created the row. <c>null</c> means the general broker — the same
        /// reading as <c>User.ProviderId</c> and <c>Trip.IntegratorId</c>.
        /// </summary>
        public int? CreatedByProviderId { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public int? UpdatedByUserId { get; set; }

        /// <summary><c>null</c> means the general broker. See <see cref="CreatedByProviderId"/>.</summary>
        public int? UpdatedByProviderId { get; set; }

        // ---- Search ------------------------------------------------------------------------------

        /// <summary>
        /// Name, city, county, ZIP, address and contact concatenated, upper-cased and with accents
        /// removed. Filled by the service on save; the term the user types is cleaned the same way
        /// before querying, which is how "gonzalez" finds "González" without depending on the
        /// database collation.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string SearchText { get; set; }
    }
}
