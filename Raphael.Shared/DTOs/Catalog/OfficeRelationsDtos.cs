namespace Raphael.Shared.DTOs.Catalog
{
    /// <summary>
    /// A clinic (Integrator account) as the office sees its relations: its row in the catalog, and the
    /// Providers it contracted (PROVIDER_ACCESS_MODEL.md §3.6). An external Provider's "my clinics"
    /// is built on the catalog row, so a clinic without one is invisible there.
    /// </summary>
    public class IntegratorRelationsDto
    {
        public int IntegratorId { get; set; }
        public int? CatalogIntegratorId { get; set; }
        public string? CatalogName { get; set; }
        public List<IntegratorProviderRowDto> Providers { get; set; } = new();
    }

    /// <summary>A Provider a clinic contracted: the catalog entry, and its account if it has one.</summary>
    public class IntegratorProviderRowDto
    {
        public int CatalogProviderId { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>The account in Raphael (what Trip.ProviderId stores); null without one.</summary>
        public int? AccountId { get; set; }
        /// <summary>"Raphael" or "External"; null without an account.</summary>
        public string? WorkMode { get; set; }
        public int ActiveUsers { get; set; }
        public DateTime ContractedAtUtc { get; set; }
    }

    /// <summary>A clinic that contracted a Provider.</summary>
    public class ProviderClinicRowDto
    {
        public int IntegratorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime ContractedAtUtc { get; set; }
    }

    /// <summary>Links a clinic's account to its row in the Integrator catalog; null unlinks it.</summary>
    public class IntegratorCatalogLinkDto
    {
        public int? CatalogIntegratorId { get; set; }
    }
}
