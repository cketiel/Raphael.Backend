using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderController : ControllerBase
    {
        private readonly IRiderService _riderService;

        public RiderController(IRiderService riderService)
        {
            _riderService = riderService;
        }

        [AllowAnonymous]
        [HttpPost("auth/identify")]
        public async Task<IActionResult> Identify([FromBody] RiderIdentifyRequest request)
        {
            var response = await _riderService.IdentifyAsync(request);
            return response == null ? Unauthorized("Patient not found in Raphael Ecosystem.") : Ok(response);
        }

        [Authorize(Roles = "Rider")]
        [HttpGet("schedules")]
        public async Task<IActionResult> GetSchedules([FromQuery] DateTime date)
        {
            var customerId = GetCurrentCustomerId();
            return Ok(await _riderService.GetMySchedulesAsync(customerId, date));
        }

        [Authorize(Roles = "Rider")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var customerId = GetCurrentCustomerId();
            return Ok(await _riderService.GetMyTripHistoryAsync(customerId, start, end));
        }

        [Authorize(Roles = "Rider")]
        [HttpGet("active-location")]
        public async Task<IActionResult> GetLocation()
        {
            var customerId = GetCurrentCustomerId();
            var locations = await _riderService.GetMyActiveVehicleLocationAsync(customerId);
            return locations.Any() ? Ok(locations) : NotFound("No active transport to track.");
        }

        [Authorize(Roles = "Rider")]
        [HttpPost("trips/{tripId}/activate-will-call")]
        public async Task<IActionResult> WillCall(int tripId)
        {
            var customerId = GetCurrentCustomerId();
            var success = await _riderService.ActivateWillCallAsync(tripId, customerId);
            return success ? Ok() : BadRequest("Could not activate Will Call.");
        }

        [Authorize(Roles = "Rider")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] CustomerCreateDto dto)
        {
            var customerId = GetCurrentCustomerId();
            var success = await _riderService.UpdateProfileAsync(customerId, dto);
            return success ? NoContent() : NotFound();
        }

        [Authorize(Roles = "Rider")]
        [HttpPost("ratings")]
        public async Task<IActionResult> PostRating([FromBody] RatingCreateDto dto)
        {
            var customerId = GetCurrentCustomerId();
            var success = await _riderService.SubmitRatingAsync(dto, customerId);
            return success ? Ok() : BadRequest("Invalid rating submission.");
        }

        private int GetCurrentCustomerId() => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
    }
}