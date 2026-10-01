using Raphael.Shared.DTOs.Catalog;

namespace Raphael.Api.Services.Catalog
{
    /// <summary>The fixed lists the catalog screens are built out of.</summary>
    public interface ICatalogLookupService
    {
        Task<IReadOnlyList<CatalogCategoryDto>> GetIntegratorCategoriesAsync(
            CancellationToken cancellationToken);

        Task<IReadOnlyList<CatalogCategoryDto>> GetProviderCategoriesAsync(
            CancellationToken cancellationToken);

        /// <summary>
        /// The counties, optionally only the ones the catalog actually uses. A dropdown of 67
        /// counties when the data covers eleven is a dropdown nobody reads.
        /// </summary>
        Task<IReadOnlyList<CountyDto>> GetCountiesAsync(
            string? state,
            bool inUseOnly,
            CancellationToken cancellationToken);
    }
}
