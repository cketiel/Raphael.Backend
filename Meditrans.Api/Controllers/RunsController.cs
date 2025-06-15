using Meditrans.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Meditrans.Api.Services;
using Meditrans.Shared.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Meditrans.Api.Controllers
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
                return NotFound($"No se pudo actualizar porque no se encontró la ruta con ID {id}.");
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
    }
}