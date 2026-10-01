using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Catalog;

namespace Raphael.Api.Services.Catalog
{
    /// <summary>
    /// The directory of entities that could carry out trips, and the way into our own list.
    /// </summary>
    /// <remarks>See <see cref="ICatalogIntegratorService"/>: the same rules, the other side.</remarks>
    public interface ICatalogProviderService
    {
        Task<CatalogPageDto<CatalogProviderListItemDto>> SearchAsync(
            CatalogSearchRequestDto request,
            CancellationToken cancellationToken);

        Task<CatalogProviderDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

        Task<CatalogProviderDto> CreateAsync(
            CatalogProviderSaveDto request,
            CancellationToken cancellationToken);

        Task<CatalogProviderDto?> UpdateAsync(
            int id,
            CatalogProviderSaveDto request,
            CancellationToken cancellationToken);

        Task<CatalogImportResultDto> ImportAsync(
            CatalogImportRequestDto<CatalogProviderImportRowDto> request,
            CancellationToken cancellationToken);

        Task<CatalogGeocodeResultDto> GeocodeAsync(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken);

        /// <summary>
        /// The provider form, filled in from the catalog row, for the user to confirm.
        /// </summary>
        /// <remarks>
        /// Nothing is written here. A provider created without a timezone falls back to the
        /// configured default, and what a pickup time means depends on that — see TIME_POLICY.md.
        /// It is a field a person should look at, not one a click should skip.
        /// </remarks>
        Task<ProviderDto?> BuildCompanyDraftAsync(int id, CancellationToken cancellationToken);
    }
}
