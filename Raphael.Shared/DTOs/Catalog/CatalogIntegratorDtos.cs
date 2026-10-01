using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs.Catalog
{
    /// <summary>A catalog integrator as the grid shows it. Deliberately narrower than the full row.</summary>
    public class CatalogIntegratorListItemDto
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

        public string? FacilityType { get; set; }
        public int? Beds { get; set; }

        public bool IsActive { get; set; }
        public bool HasCoordinates { get; set; }

        /// <summary>
        /// Whether the company asking already added this one. Not a property of the row: the
        /// same row is in one company and not in another, so it is computed per caller.
        /// </summary>
        public bool InMyCompany { get; set; }

        /// <summary>The integrator it became for this company, when it became one.</summary>
        public int? MyIntegratorId { get; set; }

        /// <summary>
        /// How many companies have added it in total. Only the general broker is told; a
        /// provider has no business knowing who else is talking to whom.
        /// </summary>
        public int? AddedByCompanyCount { get; set; }
    }

    /// <summary>The whole row, for the detail panel.</summary>
    public class CatalogIntegratorDto
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

        [StringLength(150)]
        public string? FacilityType { get; set; }

        public int? Beds { get; set; }

        [StringLength(200)]
        public string? ChainName { get; set; }

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
        public int? MyIntegratorId { get; set; }
        public int? AddedByCompanyCount { get; set; }
    }

    /// <summary>What the edit dialog sends. Nothing on it is decided by the client.</summary>
    /// <remarks>
    /// No audit fields and no coordinates: who touched it comes from the token, and coordinates
    /// come from geocoding. A client that could set either could rewrite the record's history.
    /// </remarks>
    public class CatalogIntegratorSaveDto
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

        [StringLength(150)]
        public string? FacilityType { get; set; }

        public int? Beds { get; set; }

        [StringLength(200)]
        public string? ChainName { get; set; }

        public string? Comments { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>One spreadsheet row, already mapped to our fields by the client.</summary>
    public class CatalogIntegratorImportRowDto
    {
        /// <summary>Row number in the original file, so a rejection can be pointed at.</summary>
        public int RowNumber { get; set; }

        public string? Name { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }

        /// <summary>The county as the file spells it. The server matches it against the county list.</summary>
        public string? County { get; set; }

        public string? State { get; set; }
        public string? Zip { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Website { get; set; }
        public string? ContactName { get; set; }
        public string? FacilityType { get; set; }
        public int? Beds { get; set; }
        public string? ChainName { get; set; }

        /// <summary>None of the six source files bring these, but a future one might.</summary>
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}
