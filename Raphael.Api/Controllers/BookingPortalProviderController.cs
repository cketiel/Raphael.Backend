using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.ProviderPortal;
using Raphael.Shared.DTOs.ProviderPortal;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The Booking portal of a Provider that works outside Raphael (PROVIDER_ACCESS_MODEL.md §3.5):
    /// the clinics that give it trips, rejecting, completing or marking a no-show on one of its
    /// trips, and the catalog of Integrators, whose contact details it may correct.
    /// </summary>
    /// <remarks>
    /// External Provider users only: anyone else gets a 404 from the service. Every trip is checked
    /// to be the caller's Provider's; the portal's buttons only guide.
    /// </remarks>
    [ApiController]
    [Route("api/BookingPortal/provider")]
    [Authorize]
    public class BookingPortalProviderController : ControllerBase
    {
        private readonly IProviderPortalService _portal;

        public BookingPortalProviderController(IProviderPortalService portal)
        {
            _portal = portal;
        }

        /// <summary>The clinics that contracted the caller's Provider: the Trips list's Integrator filter.</summary>
        [HttpGet("clinics")]
        [ProducesResponseType(typeof(List<ProviderClinicDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<ProviderClinicDto>>> GetClinics(CancellationToken ct) =>
            Run(() => _portal.GetClinicsAsync(ct));

        /// <summary>Accepts these Assigned trips of the caller's Provider. Answers how many were accepted.</summary>
        [HttpPost("trips/accept")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        public Task<ActionResult<int>> Accept([FromBody] List<int> tripIds, CancellationToken ct) =>
            Run(() => _portal.AcceptAsync(tripIds ?? new List<int>(), ct));

        [HttpPost("trips/{id:int}/reject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<bool>> Reject(int id, [FromBody] ProviderTripRejectDto dto, CancellationToken ct) =>
            Run(async () => { await _portal.RejectAsync(id, dto, ct); return true; }, noContent: true);

        [HttpPost("trips/{id:int}/complete")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<bool>> Complete(int id, [FromBody] ProviderTripCompleteDto dto, CancellationToken ct) =>
            Run(async () => { await _portal.CompleteAsync(id, dto, ct); return true; }, noContent: true);

        [HttpPost("trips/{id:int}/no-show")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<bool>> NoShow(int id, [FromBody] ProviderTripNoShowDto dto, CancellationToken ct) =>
            Run(async () => { await _portal.NoShowAsync(id, dto, ct); return true; }, noContent: true);

        [HttpGet("catalog/integrators/search")]
        [ProducesResponseType(typeof(ProviderPortalIntegratorPageDto), StatusCodes.Status200OK)]
        public Task<ActionResult<ProviderPortalIntegratorPageDto>> SearchIntegrators([FromQuery] ProviderPortalIntegratorSearchDto request, CancellationToken ct) =>
            Run(() => _portal.SearchIntegratorsAsync(request, ct));

        [HttpGet("catalog/integrators/{id:int}")]
        [ProducesResponseType(typeof(ProviderPortalIntegratorDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<ProviderPortalIntegratorDetailDto>> GetIntegrator(int id, CancellationToken ct) =>
            Run(async () => await _portal.GetIntegratorAsync(id, ct) ?? throw new ProviderPortalDeniedException());

        /// <remarks>A name and zip that belong to another catalog entry answer 409 (global handler).</remarks>
        [HttpPut("catalog/integrators/{id:int}")]
        [ProducesResponseType(typeof(ProviderPortalIntegratorDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public Task<ActionResult<ProviderPortalIntegratorDetailDto>> EditIntegrator(int id, [FromBody] ProviderPortalIntegratorEditDto request, CancellationToken ct) =>
            Run(() => _portal.EditIntegratorAsync(id, request, ct));

        /// <summary>Turns the service's refusals into 404 and 400; anything else reaches the global handler.</summary>
        private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work, bool noContent = false)
        {
            try
            {
                var result = await work();
                return noContent ? NoContent() : Ok(result);
            }
            catch (ProviderPortalDeniedException)
            {
                return NotFound();
            }
            catch (ProviderPortalRuleException rule)
            {
                return BadRequest(rule.Message);
            }
        }
    }
}
