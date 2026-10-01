using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Catalog;

namespace Raphael.Api.Services.Catalog
{
    /// <summary>
    /// The directory of entities that could send us trips, and the way into our own list.
    /// </summary>
    /// <remarks>
    /// Everything here is scoped to the caller's company without the caller saying so: whether a
    /// row is already "in my company" depends on who is asking, and the general broker — the user
    /// with no provider — is a company like any other for this purpose.
    /// </remarks>
    public interface ICatalogIntegratorService
    {
        Task<CatalogPageDto<CatalogIntegratorListItemDto>> SearchAsync(
            CatalogSearchRequestDto request,
            CancellationToken cancellationToken);

        Task<CatalogIntegratorDto?> GetByIdAsync(int id, CancellationToken cancellationToken);

        Task<CatalogIntegratorDto> CreateAsync(
            CatalogIntegratorSaveDto request,
            CancellationToken cancellationToken);

        Task<CatalogIntegratorDto?> UpdateAsync(
            int id,
            CatalogIntegratorSaveDto request,
            CancellationToken cancellationToken);

        /// <summary>
        /// Upserts a batch of spreadsheet rows by their natural key, so running the same file
        /// twice corrects the catalog instead of doubling it.
        /// </summary>
        Task<CatalogImportResultDto> ImportAsync(
            CatalogImportRequestDto<CatalogIntegratorImportRowDto> request,
            CancellationToken cancellationToken);

        /// <summary>Resolves coordinates for rows that have none, in one deduplicated batch.</summary>
        Task<CatalogGeocodeResultDto> GeocodeAsync(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken);

        /// <summary>
        /// The integrator form, filled in from the catalog row, for the user to confirm.
        /// </summary>
        /// <remarks>
        /// Nothing is written here. An integrator is created with a live API key against our
        /// API, and that is not something a single click should do behind somebody's back.
        /// </remarks>
        Task<IntegratorDto?> BuildCompanyDraftAsync(int id, CancellationToken cancellationToken);
    }
}
