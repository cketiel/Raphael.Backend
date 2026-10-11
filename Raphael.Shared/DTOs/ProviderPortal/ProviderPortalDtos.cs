namespace Raphael.Shared.DTOs.ProviderPortal
{
    /// <summary>A clinic that contracted the caller's Provider: what the Integrator filter lists.</summary>
    public class ProviderClinicDto
    {
        public int IntegratorId { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>The Provider will not do this trip. The reason is required: the clinic reads it.</summary>
    public class ProviderTripRejectDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>The patient was not there.</summary>
    public class ProviderTripNoShowDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// The trip was done. Picked up and dropped off are required: they are what the production
    /// report prints and what the trip is billed with. Wall-clock times of the trip's own day.
    /// </summary>
    public class ProviderTripCompleteDto
    {
        public TimeSpan? PickupArrivedAt { get; set; }
        public TimeSpan PickedUpAt { get; set; }
        public TimeSpan? DropoffArrivedAt { get; set; }
        public TimeSpan DroppedOffAt { get; set; }
    }

    /// <summary>One Integrator of the catalog, as an external Provider sees it.</summary>
    public class ProviderPortalIntegratorRowDto
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

        /// <summary>This clinic contracted the caller's Provider.</summary>
        public bool IsMyClinic { get; set; }
    }

    public class ProviderPortalIntegratorDetailDto : ProviderPortalIntegratorRowDto
    {
        public string? Address { get; set; }
        public string? Zip { get; set; }
        public string? ContactName { get; set; }
        public string? FacilityType { get; set; }
        public int? Beds { get; set; }
        public string? ChainName { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }

    public class ProviderPortalIntegratorSearchDto
    {
        public string? Term { get; set; }
        public int? GroupId { get; set; }
        /// <summary>Only the clinics that contracted the caller's Provider.</summary>
        public bool? Mine { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class ProviderPortalIntegratorPageDto
    {
        public List<ProviderPortalIntegratorRowDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int MineCount { get; set; }
        public List<Raphael.Shared.DTOs.Catalog.CatalogFacetValueDto> Groups { get; set; } = new();
    }

    /// <summary>The contact details an external Provider may correct on any Integrator of the catalog.</summary>
    public class ProviderPortalIntegratorEditDto
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
}
