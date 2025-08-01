using Raphael.Shared.Entities;
using Raphael.Shared.DTOs;
using Raphael.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CapacityDetailTypeController : ControllerBase
    {
        private readonly ICapacityDetailTypeService _service;

        public CapacityDetailTypeController(ICapacityDetailTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<CapacityDetailType>>> GetAll()
        {
            return Ok(await _service.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CapacityDetailType>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<CapacityDetailType>> Create(CapacityDetailTypeDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CapacityDetailType>> Update(int id, CapacityDetailTypeDto dto)
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

