using Raphael.Api.Services;
using Raphael.Shared.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleTypesController : ControllerBase
    {
        private readonly IVehicleTypeService _service;

        public VehicleTypesController(IVehicleTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<VehicleType>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleType>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<VehicleType>> Create(VehicleType dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<VehicleType>> Update(int id, VehicleType dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
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

