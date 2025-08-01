using Meditrans.Api.Services;
using Meditrans.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Meditrans.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public SchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet("by-run-login")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedulesByRunLogin([FromQuery] string runLogin, [FromQuery] DateTime date)
        {
            var schedules = await _scheduleService.GetSchedulesByRunLoginAndDateAsync(runLogin, date);
            return Ok(schedules);
        }

        [HttpGet("by-route")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedules([FromQuery] int vehicleRouteId, [FromQuery] DateTime date)
        {
            var schedules = await _scheduleService.GetSchedulesByRouteAndDateAsync(vehicleRouteId, date);
            return Ok(schedules);
        }

        [HttpGet("unscheduled")]
        public async Task<ActionResult<IEnumerable<UnscheduledTripDto>>> GetUnscheduledTrips([FromQuery] DateTime date)
        {
            var trips = await _scheduleService.GetUnscheduledTripsByDateAsync(date);
            return Ok(trips);
        }

        [HttpPost("route")]
        public async Task<IActionResult> RouteTrips([FromBody] RouteTripRequest request)
        {
            try
            {
                await _scheduleService.RouteTripAsync(request);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("cancel-route")]
        public async Task<IActionResult> CancelRoute([FromBody] CancelRouteRequest request)
        {
            try
            {
                await _scheduleService.CancelRouteForTripAsync(request.ScheduleId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ScheduleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _scheduleService.UpdateAsync(id, dto);
            if (!success)
            {
                return NotFound($"Could not update because the schedule with ID {id} was not found.");
            }
            // 204 NoContent is the standard response for a successful PUT that returns no content.
            return NoContent();
        }
    }
}
