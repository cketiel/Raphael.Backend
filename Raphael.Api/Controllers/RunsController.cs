using Raphael.Api.Services.Auth;
using Raphael.Api.Attributes;
using Raphael.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RunsController : ControllerBase
    {
        private readonly IRunService _service;

        public RunsController(IRunService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleRoute>>> GetAll()
        {
            var routes = await _service.GetAllAsync();
            return Ok(routes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleRoute>> GetById(int id)
        {
            var route = await _service.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound($"Route with ID not found {id}.");
            }
            return Ok(route);
        }

        // [FromBody] is used to indicate that the DTO comes in the body of the request
        [HttpPost]
        public async Task<ActionResult<VehicleRoute>> Create([FromBody] VehicleRouteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdRoute = await _service.CreateAsync(dto);
            // The entire entity, including its new ID, is returned so the client can use it.
            return CreatedAtAction(nameof(GetById), new { id = createdRoute.Id }, createdRoute);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] VehicleRouteDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // RE-052: the preview the user confirmed, if any, and the request's own cancellation, so a
            // stop from the Desktop rolls the whole save back.
            var confirmed = Request.Headers["X-Garage-Preview"].ToString();
            var result = await _service.UpdateAsync(id, dto,
                string.IsNullOrWhiteSpace(confirmed) ? null : confirmed, HttpContext.RequestAborted);
            if (result == null)
            {
                return NotFound($"Could not update because the route with ID {id} was not found.");
            }

            // RE-052: a new garage answers with what it did to the planned days, so the Desktop can
            // show it when it differs from the preview. Anything else stays the 204 it always was.
            if (result.GarageChanged) return Ok(result);
            return NoContent();
        }

        /// <summary>
        /// What giving this route a new garage would do to the days already planned on it (RE-052).
        /// Nothing is saved.
        /// </summary>
        [HttpPost("{id}/garage-change/preview")]
        public async Task<ActionResult<GarageChangePreviewDto>> PreviewGarageChange(
            int id,
            [FromBody] GarageChangeRequestDto request,
            [FromServices] Raphael.Api.Services.Routing.IGarageChangeService garage,
            CancellationToken ct)
        {
            if (request == null) return BadRequest();

            try
            {
                return Ok(await garage.PreviewAsync(id, request, ct));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
            {
                return NotFound($"Could not delete because path with ID was not found {id}.");
            }
            return NoContent();
        }

        /// <summary>
        /// Cancels a vehicle route by setting its end date.
        /// </summary>
        /// <param name="id">The ID of the route to cancel.</param>
        /// <returns>NoContent if the cancellation is successful, or NotFound if the route does not exist.</returns>
        [HttpPatch("{id}/cancel")] // We use PATCH and a descriptive path. Ej: PATCH /api/runs/123/cancel
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Cancel(int id)
        {
            var success = await _service.CancelAsync(id);
            if (!success)
            {
                return NotFound($"Could not cancel because route with ID {id} was not found.");
            }

            // 204 NoContent is the standard response for a successful operation that returns no data.
            return NoContent();
        }

        [AllowApps(ClientApps.Desktop, ClientApps.Driver)]
        [HttpGet("driver/{driverId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VehicleRoute))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<VehicleRoute>> GetByDriverId(int driverId)
        {
            var route = await _service.GetActiveRunByDriverIdAsync(driverId);
            if (route == null)
            {
                return NotFound($"No active route found for driver with ID {driverId}.");
            }
            return Ok(route);
        }
    }
}
