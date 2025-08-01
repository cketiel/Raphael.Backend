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

            var success = await _service.UpdateAsync(id, dto);
            if (!success)
            {
                return NotFound($"Could not update because the route with ID {id} was not found.");
            }
            // 204 NoContent is the standard response for a successful PUT that returns no content.
            return NoContent();
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
    }
}