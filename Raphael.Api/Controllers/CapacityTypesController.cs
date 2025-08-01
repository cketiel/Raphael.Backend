using Raphael.Shared.Dtos;
using Raphael.Shared.Entities;
using Raphael.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CapacityTypesController : ControllerBase
    {
        private readonly CapacityTypeService _service;

        public CapacityTypesController(CapacityTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<CapacityType>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CapacityType>> GetById(int id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        [HttpPost]
        public async Task<ActionResult<CapacityType>> Create(CapacityTypeDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CapacityType>> Update(int id, CapacityTypeDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}
