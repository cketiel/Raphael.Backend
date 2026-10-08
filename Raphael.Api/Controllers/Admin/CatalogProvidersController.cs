using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Catalog;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Catalog;

using Raphael.Api.Attributes;
namespace Raphael.Api.Controllers.Admin
{
    /// <summary>
    /// The directory of entities that could carry out trips: NEMT companies, NEMT brokers,
    /// private ambulance operators.
    /// </summary>
    /// <remarks>
    /// ⚠️ Administrators only, checked here and not only in the client. See
    /// <see cref="CatalogIntegratorsController"/>, which this mirrors in every respect.
    /// </remarks>
    [ApiController]
    [Route("api/admin/catalog/providers")]
    [Authorize(Roles = "1")]
    // Office administration: role 1 is also a clinic's admin, who must not reach it.
    [NotForClinicUsers]
    public sealed class CatalogProvidersController : ControllerBase
    {
        private const int MaxImportRows = 200;
        private const int MaxGeocodeIds = 100;

        private readonly ICatalogProviderService _catalog;

        public CatalogProvidersController(ICatalogProviderService catalog)
        {
            _catalog = catalog;
        }

        /// <summary>Searches the catalog and returns one page of it.</summary>
        /// <remarks>A POST for the same reason as the integrator side: no names in URLs.</remarks>
        [HttpPost("search")]
        public async Task<ActionResult<CatalogPageDto<CatalogProviderListItemDto>>> Search(
            [FromBody] CatalogSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            return Ok(await _catalog.SearchAsync(request, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CatalogProviderDto>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var row = await _catalog.GetByIdAsync(id, cancellationToken);

            return row is null ? NotFound() : Ok(row);
        }

        [HttpPost]
        public async Task<ActionResult<CatalogProviderDto>> Create(
            [FromBody] CatalogProviderSaveDto request,
            CancellationToken cancellationToken)
        {
            var created = await _catalog.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CatalogProviderDto>> Update(
            int id,
            [FromBody] CatalogProviderSaveDto request,
            CancellationToken cancellationToken)
        {
            var updated = await _catalog.UpdateAsync(id, request, cancellationToken);

            return updated is null ? NotFound() : Ok(updated);
        }

        [HttpPost("import")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        public async Task<ActionResult<CatalogImportResultDto>> Import(
            [FromBody] CatalogImportRequestDto<CatalogProviderImportRowDto> request,
            CancellationToken cancellationToken)
        {
            if (request.Rows.Count > MaxImportRows)
            {
                return BadRequest($"A batch holds at most {MaxImportRows} rows.");
            }

            var result = await _catalog.ImportAsync(request, cancellationToken);

            return result.Failed > 0
                ? StatusCode(StatusCodes.Status207MultiStatus, result)
                : Ok(result);
        }

        [HttpPost("geocode")]
        public async Task<ActionResult<CatalogGeocodeResultDto>> Geocode(
            [FromBody] CatalogGeocodeRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request.Ids.Count > MaxGeocodeIds)
            {
                return BadRequest(
                    $"At most {MaxGeocodeIds} rows at a time. The daily Geocoding quota is 1,500.");
            }

            return Ok(await _catalog.GeocodeAsync(request.Ids, cancellationToken));
        }

        /// <summary>
        /// The provider form, filled in from the catalog row, for the user to confirm.
        /// </summary>
        /// <remarks>
        /// Writes nothing, and deliberately leaves the timezone empty: it is the one field a
        /// directory cannot tell us, and what a pickup time means depends on it.
        /// </remarks>
        [HttpGet("{id:int}/company-draft")]
        public async Task<ActionResult<ProviderDto>> CompanyDraft(
            int id,
            CancellationToken cancellationToken)
        {
            var draft = await _catalog.BuildCompanyDraftAsync(id, cancellationToken);

            return draft is null ? NotFound() : Ok(draft);
        }
    }
}
