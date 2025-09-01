using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class UnitsController : ControllerBase
    {
        private readonly UnitService _service;

        public UnitsController(UnitService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<Unit>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Unit>> GetById(int id)
        {
            var unit = await _service.GetByIdAsync(id);
            if (unit == null) return NotFound();
            return Ok(unit);
        }

        [HttpPost]
        public async Task<ActionResult<Unit>> Create(UnitDto dto)
        {
            var createdUnit = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = createdUnit.Id }, createdUnit);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Unit>> Update(int id, UnitDto dto)
        {
            var updatedUnit = await _service.UpdateAsync(id, dto);
            if (updatedUnit == null) return NotFound();
            return Ok(updatedUnit);
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