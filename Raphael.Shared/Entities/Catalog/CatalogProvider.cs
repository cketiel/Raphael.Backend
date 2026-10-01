using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>
    /// An entity that could carry out trips — an NEMT company, an NEMT broker, a private ambulance
    /// operator — whether or not we work with it yet.
    /// </summary>
    /// <remarks>
    /// This is not a <see cref="Provider"/>. A Provider is somebody we already work with, whose
    /// vehicles do our trips and whose timezone decides what a pickup time means. This is the
    /// directory we work the phone from. A row is never deleted; it goes <see cref="IsActive"/>
    /// false. See <see cref="CatalogIntegrator"/> for the rest of the reasoning, which is the same.
    /// </remarks>
    public class CatalogProvider
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public CatalogProviderCategory Category { get; set; }

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
        /// The county as the source file spelled it. The NEMT file brings no county column at all,
        /// so most of those rows have neither this nor <see cref="CountyId"/> until somebody fills
        /// them in or the row is geocoded.
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
        /// on save. See <see cref="CatalogIntegrator.PhoneDigits"/>.
        /// </summary>
        [MaxLength(20)]
        public string? PhoneDigits { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Website { get; set; }

        /// <summary>Director or administrator, whoever the source named.</summary>
        [MaxLength(200)]
        public string? ContactName { get; set; }

        // ---- Particular to this kind of entity ----------------------------------------------

        /// <summary>National Provider Identifier. The closest thing these rows have to a real key.</summary>
        [MaxLength(20)]
        public string? Npi { get; set; }

        /// <summary>Whether NEMT is their main taxonomy, as the NPPES export reports it.</summary>
        public bool? IsPrimaryNemt { get; set; }

        /// <summary>
        /// When the source last verified the row. Their date, not ours — a row can be freshly
        /// imported and still say it was verified two years ago, and that is the useful fact.
        /// </summary>
        public DateTime? SourceUpdatedOn { get; set; }

        /// <summary>EMS licence number, for ambulance operators.</summary>
        [MaxLength(60)]
        public string? EmsLicense { get; set; }

        /// <summary>BLS, ALS, ALS/T — copied literally from the state report, not interpreted.</summary>
        [MaxLength(60)]
        public string? ServiceLevel { get; set; }

        public DateTime? LicenseExpiresOn { get; set; }

        /// <summary>The plan or segment a broker covers.</summary>
        [MaxLength(300)]
        public string? PlanSegment { get; set; }

        /// <summary>What area they actually serve, which is not always where their office is.</summary>
        [MaxLength(300)]
        public string? CoverageArea { get; set; }

        /// <summary>The line a broker publishes for providers, separate from their general one.</summary>
        [MaxLength(300)]
        public string? ProviderContact { get; set; }

        /// <summary>Where the source's claim came from, so it can be checked rather than believed.</summary>
        [MaxLength(500)]
        public string? EvidenceNote { get; set; }

        // ---- Working the contact ------------------------------------------------------------

        /// <summary>Free notes on the conversations.</summary>
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
        /// Unique. See <see cref="CatalogIntegrator.MatchKey"/>.
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
        /// removed. See <see cref="CatalogIntegrator.SearchText"/>.
        /// </summary>
        [Required]
        [MaxLength(1000)]
        public string SearchText { get; set; }
    }
}
