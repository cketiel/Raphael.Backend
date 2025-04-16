using Meditrans.TripsService.DTOs;
using Microsoft.AspNetCore.Mvc;
using Meditrans.TripsService.Services;
using Meditrans.Shared.Entities;
using System.Threading.Tasks;

namespace Meditrans.TripsService.Controllers
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
        public async Task<ActionResult<List<VehicleRoute>>> GetAll()
        {
            var routes = await _service.GetAllAsync();
            return Ok(routes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleRoute>> GetById(int id)
        {
            var route = await _service.GetByIdAsync(id);
            if (route == null) return NotFound();
            return Ok(route);
        }

        [HttpPost]
        public async Task<ActionResult<VehicleRoute>> Create(RunDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<VehicleRoute>> Update(int id, RunDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (!updated) return NotFound();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }
    }

}
