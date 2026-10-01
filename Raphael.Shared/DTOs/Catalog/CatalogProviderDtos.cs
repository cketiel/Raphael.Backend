using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs.Catalog
{
    /// <summary>A catalog provider as the grid shows it.</summary>
    public class CatalogProviderListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int CategoryId { get; set; }
        public string CategoryCode { get; set; }

        public string? City { get; set; }
        public string? CountyName { get; set; }
        public string? State { get; set; }
        public string? Zip { get; set; }

        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? ContactName { get; set; }

        public string? Npi { get; set; }
        public string? ServiceLevel { get; set; }
        public DateTime? LicenseExpiresOn { get; set; }

        public bool IsActive { get; set; }
        public bool HasCoordinates { get; set; }

        /// <summary>Whether the company asking already added this one. Computed per caller.</summary>
        public bool InMyCompany { get; set; }

        /// <summary>The provider it became for this company, when it became one.</summary>
        public int? MyProviderId { get; set; }

        /// <summary>Total companies that added it. Only the general broker is told.</summary>
        public int? AddedByCompanyCount { get; set; }
    }

    /// <summary>The whole row, for the detail panel.</summary>
    public class CatalogProviderDto
    {
        public int Id { get; set; }

        [Required]
        public int CategoryId { get; set; }
        public string? CategoryCode { get; set; }

        [Required(ErrorMessage = "The name is required")]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        public int? CountyId { get; set; }
        public string? CountyName { get; set; }
        public string? CountyRaw { get; set; }

        [StringLength(2)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? Zip { get; set; }

        [StringLength(40)]
        public string? Phone { get; set; }

        [StringLength(200)]
        [EmailAddress(ErrorMessage = "That does not look like an email address")]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Website { get; set; }

        [StringLength(200)]
        public string? ContactName { get; set; }

        [StringLength(20)]
        public string? Npi { get; set; }

        public bool? IsPrimaryNemt { get; set; }

        public DateTime? SourceUpdatedOn { get; set; }

        [StringLength(60)]
        public string? EmsLicense { get; set; }

        [StringLength(60)]
        public string? ServiceLevel { get; set; }

        public DateTime? LicenseExpiresOn { get; set; }

        [StringLength(300)]
        public string? PlanSegment { get; set; }

        [StringLength(300)]
        public string? CoverageArea { get; set; }

        [StringLength(300)]
        public string? ProviderContact { get; set; }

        [StringLength(500)]
        public string? EvidenceNote { get; set; }

        public string? Comments { get; set; }

        public bool IsActive { get; set; } = true;

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public int GeocodeStatus { get; set; }
        public DateTime? GeocodedAtUtc { get; set; }

        public string? SourceFile { get; set; }
        public int? SourceRow { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }
        public string? CreatedByUserName { get; set; }

        /// <summary>The company that created it. Null means the general broker.</summary>
        public int? CreatedByProviderId { get; set; }
        public string? CreatedByProviderName { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
        public int? UpdatedByProviderId { get; set; }
        public string? UpdatedByProviderName { get; set; }

        public bool InMyCompany { get; set; }
        public int? MyProviderId { get; set; }
        public int? AddedByCompanyCount { get; set; }
    }

    /// <summary>What the edit dialog sends. See <see cref="CatalogIntegratorSaveDto"/>.</summary>
    public class CatalogProviderSaveDto
    {
        [Required]
        public int CategoryId { get; set; }

        [Required(ErrorMessage = "The name is required")]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        public int? CountyId { get; set; }

        [StringLength(2)]
        public string? State { get; set; }

        [StringLength(20)]
        public string? Zip { get; set; }

        [StringLength(40)]
        public string? Phone { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Website { get; set; }

        [StringLength(200)]
        public string? ContactName { get; set; }

        [StringLength(20)]
        public string? Npi { get; set; }

        public bool? IsPrimaryNemt { get; set; }

        public DateTime? SourceUpdatedOn { get; set; }

        [StringLength(60)]
        public string? EmsLicense { get; set; }

        [StringLength(60)]
        public string? ServiceLevel { get; set; }

        public DateTime? LicenseExpiresOn { get; set; }

        [StringLength(300)]
        public string? PlanSegment { get; set; }

        [StringLength(300)]
        public string? CoverageArea { get; set; }

        [StringLength(300)]
        public string? ProviderContact { get; set; }

        [StringLength(500)]
        public string? EvidenceNote { get; set; }

        public string? Comments { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>One spreadsheet row, already mapped to our fields by the client.</summary>
    public class CatalogProviderImportRowDto
    {
        /// <summary>Row number in the original file, so a rejection can be pointed at.</summary>
        public int RowNumber { get; set; }

        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }

        /// <summary>The NEMT file has no county column; these arrive null and stay null.</summary>
        public string? County { get; set; }

        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? ContactName { get; set; }

        public string? Npi { get; set; }
        public bool? IsPrimaryNemt { get; set; }
        public DateTime? SourceUpdatedOn { get; set; }
        public string? EmsLicense { get; set; }
        public string? ServiceLevel { get; set; }
        public DateTime? LicenseExpiresOn { get; set; }
        public string? PlanSegment { get; set; }
        public string? CoverageArea { get; set; }
        public string? ProviderContact { get; set; }
        public string? EvidenceNote { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
