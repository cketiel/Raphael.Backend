using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Catalog;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Catalog;

using Raphael.Api.Attributes;
using Raphael.Api.Services.Auth;
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
    // Any admin reads and searches it, and a client company's admin edits contact data only
    // (decision D3, the same rule C as clinics and external Providers). Creating, importing,
    // geocoding and turning a row into an account are the platform's. Role 1 is also a clinic's
    // admin, who must not reach it.
    [Authorize(Roles = "1")]
    [NotForClinicUsers]
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
        private readonly Raphael.Shared.Interfaces.ICurrentUserService _currentUser;

        public CatalogIntegratorsController(ICatalogIntegratorService catalog, Raphael.Shared.Interfaces.ICurrentUserService currentUser)
        {
            _catalog = catalog;
            _currentUser = currentUser;
        }

        /// <summary>Raphael's own office: an admin of no Provider and no Integrator.</summary>
        private bool IsPlatform => _currentUser.ProviderId == null && _currentUser.IntegratorId == null;

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
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
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
            // A client company's admin changes contact data only; everything else keeps its value.
            if (!IsPlatform)
            {
                var current = await _catalog.GetByIdAsync(id, cancellationToken);
                if (current is null) return NotFound();
                request = ContactOnly(current, request);
            }

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
[Authorize(Policy = AuthPolicies.PlatformAdmin)]
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
[Authorize(Policy = AuthPolicies.PlatformAdmin)]
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
[Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [HttpGet("{id:int}/company-draft")]
        public async Task<ActionResult<IntegratorDto>> CompanyDraft(
            int id,
            CancellationToken cancellationToken)
        {
            var draft = await _catalog.BuildCompanyDraftAsync(id, cancellationToken);

            return draft is null ? NotFound() : Ok(draft);
        }

        /// <summary>
        /// The stored row with only the contact fields taken from the request: name, address,
        /// city, state, zip, phone, e-mail, website and contact person (rule C).
        /// </summary>
        private static CatalogIntegratorSaveDto ContactOnly(CatalogIntegratorDto current, CatalogIntegratorSaveDto request)
        {
            var merged = new CatalogIntegratorSaveDto();
            foreach (var target in typeof(CatalogIntegratorSaveDto).GetProperties().Where(p => p.CanWrite))
            {
                var source = typeof(CatalogIntegratorDto).GetProperty(target.Name);
                if (source != null && target.PropertyType.IsAssignableFrom(source.PropertyType))
                    target.SetValue(merged, source.GetValue(current));
            }

            merged.Name = request.Name;
            merged.Address = request.Address;
            merged.City = request.City;
            merged.State = request.State;
            merged.Zip = request.Zip;
            merged.Phone = request.Phone;
            merged.Email = request.Email;
            merged.Website = request.Website;
            merged.ContactName = request.ContactName;
            return merged;
        }
    }
}
