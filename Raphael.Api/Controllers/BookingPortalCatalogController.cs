using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Catalog;
using Raphael.Shared.DTOs.Catalog;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The catalog as a clinic sees it in the Booking Portal: search it, read an entry, contract a
    /// Provider, correct a contracted Provider's contact details, and list the ones a trip can go to.
    /// </summary>
    /// <remarks>
    /// Clinic users only: every call needs the caller's integrator, and an office user gets a 404.
    /// Contracting and editing are for a clinic's admin (role 1 with an integrator).
    /// The office administers the catalog through <c>api/admin/catalog</c>, which clinics never reach.
    /// </remarks>
    [ApiController]
    [Route("api/BookingPortal/catalog")]
    [Authorize(Roles = "6,1,3")]
    public class BookingPortalCatalogController : ControllerBase
    {
        private readonly IPortalCatalogService _catalog;

        public BookingPortalCatalogController(IPortalCatalogService catalog)
        {
            _catalog = catalog;
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<PortalCatalogCategoryDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<PortalCatalogCategoryDto>>> GetCategories(CancellationToken ct) =>
            Run(async () => await _catalog.GetCategoriesAsync(ct));

        [HttpGet("providers/search")]
        [ProducesResponseType(typeof(PortalCatalogPageDto<PortalCatalogProviderRowDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<PortalCatalogPageDto<PortalCatalogProviderRowDto>>> SearchProviders([FromQuery] PortalCatalogSearchDto request, CancellationToken ct) =>
            Run(async () => await _catalog.SearchProvidersAsync(request, ct));

        [HttpGet("providers/assignable")]
        [ProducesResponseType(typeof(List<AssignableProviderDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<AssignableProviderDto>>> GetAssignable(CancellationToken ct) =>
            Run(async () => await _catalog.GetAssignableProvidersAsync(ct));

        [HttpGet("providers/{id:int}")]
        [ProducesResponseType(typeof(PortalCatalogProviderDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<PortalCatalogProviderDetailDto>> GetProvider(int id, CancellationToken ct) =>
            Run(async () => await _catalog.GetProviderAsync(id, ct) ?? throw new PortalCatalogDeniedException(notFound: true));

        [HttpPost("providers/{id:int}/contract")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<ActionResult<bool>> Contract(int id, CancellationToken ct) =>
            Run(async () => { await _catalog.ContractAsync(id, ct); return true; }, noContent: true);

        [HttpDelete("providers/{id:int}/contract")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public Task<ActionResult<bool>> Uncontract(int id, CancellationToken ct) =>
            Run(async () => { await _catalog.UncontractAsync(id, ct); return true; }, noContent: true);

        /// <remarks>A name and zip that belong to another catalog entry answer 409 (global handler).</remarks>
        [HttpPut("providers/{id:int}")]
        [ProducesResponseType(typeof(PortalCatalogProviderDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public Task<ActionResult<PortalCatalogProviderDetailDto>> EditProvider(int id, [FromBody] PortalCatalogProviderEditDto request, CancellationToken ct) =>
            Run(async () => await _catalog.EditProviderAsync(id, request, ct));

        /// <summary>Turns the service's refusals into 403, 404 and 400; anything else reaches the global handler.</summary>
        private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work, bool noContent = false)
        {
            try
            {
                var result = await work();
                return noContent ? NoContent() : Ok(result);
            }
            catch (PortalCatalogDeniedException denied)
            {
                return denied.NotFound ? NotFound() : Forbid();
            }
            catch (PortalCatalogRuleException rule)
            {
                return BadRequest(rule.Message);
            }
        }
    }
}
