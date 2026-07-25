using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers
{
    [Authorize(Roles = "6,1,3")] // Booking
    [ApiController]
    [Route("api/[controller]")]
    public class BookingPortalController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly ICurrentUserService _currentUserService;

        public BookingPortalController(ITripService tripService, ICurrentUserService currentUserService)
        {
            _tripService = tripService;
            _currentUserService = currentUserService;
        }

        private int CurrentIntegratorId => _currentUserService.IntegratorId ?? throw new UnauthorizedAccessException();

        [HttpPost("sync-single")]
        public async Task<IActionResult> SyncSingle([FromForm] PortalTripDto trip)
        {
            if (trip == null) return BadRequest("Trip data is required.");

            try
            {
                // Llamamos al método pasando una lista de un solo elemento
                var results = await _tripService.UpsertPortalTripsAsync(new List<PortalTripDto> { trip }, CurrentIntegratorId);
                return Ok(new { Success = true, TripIds = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("my-trips")]
        public async Task<IActionResult> GetMyTrips([FromQuery] DateTime startDate, [FromQuery] DateTime? endDate)
        {
            // El Global Query Filter en RaphaelContext ya se encarga de filtrar por IntegratorId
            var trips = await _tripService.GetByDateRangeAsync(startDate, endDate ?? startDate);
            return Ok(trips);
        }

        // 3. Cancelar múltiples viajes
        [HttpPost("cancel-multiple")]
        public async Task<IActionResult> CancelMultiple([FromBody] List<string> externalIds)
        {
            var count = await _tripService.CancelIntegrationTripsAsync(externalIds, CurrentIntegratorId);
            return Ok(new { Success = true, CancelledCount = count });
        }

        [HttpGet("my-funding-source")]
        public async Task<IActionResult> GetMyFundingSource([FromServices] IIntegratorService integratorService)
        {
            var fundingSource = await integratorService.GetFundingSourceByIntegratorIdAsync(CurrentIntegratorId);
            if (fundingSource == null) return NotFound("No funding source linked to this integrator.");

            return Ok(fundingSource);
        }
    }
}
