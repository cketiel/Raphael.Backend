using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Catalog;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Catalog;

namespace Raphael.Api.Controllers.Admin
{
    /// <summary>
    /// The directory of entities that could send us trips: nursing homes, assisted living
    /// residences, hospitals — the ones we work with and the ones we are still talking to.
    /// </summary>
    /// <remarks>
    /// ⚠️ Administrators only, checked here and not only in the client. The Desktop hides the
    /// Admin menu from everyone but role 1, and that is a menu, not a lock: the API is reachable
    /// on its own. Role 1 is Administrator, issued at login as the numeric RoleId — the same
    /// check as <see cref="SystemSettingsController"/>.
    ///
    /// <para>
    /// The catalog has no DELETE. An entity we stopped being interested in is marked inactive;
    /// the reason somebody looked at it once outlives the interest.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/admin/catalog/integrators")]
    [Authorize(Roles = "1")]
    public sealed class CatalogIntegratorsController : ControllerBase
    {
        /// <summary>
        /// Biggest import batch the server will take at once. The client sends a file in
        /// batches of 100 — see IMPORT_SPEC.md — and this is the ceiling, not the size.
        /// </summary>
        private const int MaxImportRows = 200;

        /// <summary>
        /// Most rows one geocoding call will take. The daily Geocoding quota is 1,500 and the
        /// catalog is thousands of rows, so a request for the whole thing is refused rather
        /// than half-served.
        /// </summary>
        private const int MaxGeocodeIds = 100;

        private readonly ICatalogIntegratorService _catalog;

        public CatalogIntegratorsController(ICatalogIntegratorService catalog)
        {
            _catalog = catalog;
        }

        /// <summary>Searches the catalog and returns one page of it.</summary>
        /// <remarks>
        /// A POST because the search can carry the name of a contact person, and a name in a
        /// query string ends up in the browser history, in the Referer sent to third parties
        /// and in the logs of every proxy on the way — see SECURITY_FINDINGS.md F12.
        /// </remarks>
        [HttpPost("search")]
        public async Task<ActionResult<CatalogPageDto<CatalogIntegratorListItemDto>>> Search(
            [FromBody] CatalogSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            return Ok(await _catalog.SearchAsync(request, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<CatalogIntegratorDto>> GetById(
            int id,
            CancellationToken cancellationToken)
        {
            var row = await _catalog.GetByIdAsync(id, cancellationToken);

            return row is null ? NotFound() : Ok(row);
        }

        /// <summary>Adds an entity somebody found outside the imported files.</summary>
        [HttpPost]
        public async Task<ActionResult<CatalogIntegratorDto>> Create(
            [FromBody] CatalogIntegratorSaveDto request,
            CancellationToken cancellationToken)
        {
            var created = await _catalog.CreateAsync(request, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<CatalogIntegratorDto>> Update(
            int id,
            [FromBody] CatalogIntegratorSaveDto request,
            CancellationToken cancellationToken)
        {
            var updated = await _catalog.UpdateAsync(id, request, cancellationToken);

            return updated is null ? NotFound() : Ok(updated);
        }

        /// <summary>
        /// Takes one batch of rows read from a spreadsheet and upserts them by their natural key.
        /// </summary>
        /// <remarks>
        /// Returns 207 when some rows were rejected and others were not: the client needs the
        /// per-row verdict either way, and failing the whole batch over one bad row would mean
        /// re-sending ninety-nine good ones.
        /// </remarks>
        [HttpPost("import")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status207MultiStatus)]
        public async Task<ActionResult<CatalogImportResultDto>> Import(
            [FromBody] CatalogImportRequestDto<CatalogIntegratorImportRowDto> request,
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

        /// <summary>Resolves coordinates for the rows asked for, in one deduplicated batch.</summary>
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
        /// The integrator form, filled in from the catalog row, for the user to confirm.
        /// </summary>
        /// <remarks>
        /// This writes nothing. The client shows the filled-in form; saving it is an ordinary
        /// POST to <c>api/integrators</c>, which is where the API key is generated and where the
        /// owning company is taken from the token.
        /// </remarks>
        [HttpGet("{id:int}/company-draft")]
        public async Task<ActionResult<IntegratorDto>> CompanyDraft(
            int id,
            CancellationToken cancellationToken)
        {
            var draft = await _catalog.BuildCompanyDraftAsync(id, cancellationToken);

            return draft is null ? NotFound() : Ok(draft);
        }
    }
}
