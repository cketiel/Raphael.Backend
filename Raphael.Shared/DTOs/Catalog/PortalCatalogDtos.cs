namespace Raphael.Shared.DTOs.Catalog
{
    /// <summary>
    /// A catalog category a clinic can see in the Booking Portal, with its groups.
    /// </summary>
    /// <remarks>
    /// The catalog will have five or six fixed categories, some with groups and some without
    /// (CATALOG_MODEL.md §4.4). Today a clinic sees only <c>providers</c>.
    /// </remarks>
    public class PortalCatalogCategoryDto
    {
        /// <summary>Stable key the portal routes on: <c>providers</c>.</summary>
        public string Key { get; set; } = string.Empty;

        public string NameEn { get; set; } = string.Empty;

        public string NameEs { get; set; } = string.Empty;

        /// <summary>Empty when the category has no groups.</summary>
        public List<CatalogCategoryDto> Groups { get; set; } = new();
    }

    /// <summary>Filters of a catalog search in the portal.</summary>
    public class PortalCatalogSearchDto
    {
        public string? Term { get; set; }

        public int? GroupId { get; set; }

        public int? CountyId { get; set; }

        public string? City { get; set; }

        /// <summary>True: only the ones this clinic contracted. False: only the ones it did not.</summary>
        public bool? Contracted { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 25;
    }

    /// <summary>One page of a portal catalog search, with what the filters can offer.</summary>
    public class PortalCatalogPageDto<T>
    {
        public List<T> Items { get; set; } = new();

        public int TotalCount { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public List<CatalogFacetValueDto> Groups { get; set; } = new();

        public List<CatalogFacetValueDto> Counties { get; set; } = new();

        public List<CatalogFacetValueDto> Cities { get; set; } = new();

        /// <summary>How many of the matching entries this clinic has contracted.</summary>
        public int ContractedCount { get; set; }
    }

    /// <summary>A catalog Provider as a row of the portal's list.</summary>
    public class PortalCatalogProviderRowDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int GroupId { get; set; }

        public string GroupNameEn { get; set; } = string.Empty;

        public string GroupNameEs { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? County { get; set; }

        public string? State { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Website { get; set; }

        public bool IsActive { get; set; }

        /// <summary>This clinic contracted it.</summary>
        public bool Contracted { get; set; }

        /// <summary>It has an account in Raphael: trips can be given to it.</summary>
        public bool OperatesInRaphael { get; set; }
    }

    /// <summary>A catalog Provider's file in the portal.</summary>
    public class PortalCatalogProviderDetailDto : PortalCatalogProviderRowDto
    {
        public string? Address { get; set; }

        public string? Zip { get; set; }

        public string? ContactName { get; set; }

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        // Regulatory and coverage fields: read-only for a clinic.
        public string? Npi { get; set; }

        public string? ServiceLevel { get; set; }

        public string? CoverageArea { get; set; }

        public string? EmsLicense { get; set; }

        public DateTime? LicenseExpiresOn { get; set; }

        public string? PlanSegment { get; set; }

        /// <summary>The caller may edit its contact details: any clinic admin, contracted or not.</summary>
        public bool CanEdit { get; set; }

        /// <summary>The caller may contract it or remove it: a clinic admin.</summary>
        public bool CanContract { get; set; }
    }

    /// <summary>The contact details a clinic admin may change on a Provider it contracted.</summary>
    public class PortalCatalogProviderEditDto
    {
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Zip { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Website { get; set; }

        public string? ContactName { get; set; }
    }

    /// <summary>A Provider a trip can be given to: contracted, active and with an account.</summary>
    public class AssignableProviderDto
    {
        /// <summary>The account id: what <c>Trip.ProviderId</c> stores.</summary>
        public int ProviderId { get; set; }

        public string Name { get; set; } = string.Empty;
    }
}
