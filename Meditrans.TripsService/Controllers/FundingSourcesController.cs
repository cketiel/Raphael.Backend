
using Raphael.Shared.Dtos;
using Raphael.Shared.Entities;
using Raphael.TripsService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Raphael.TripsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FundingSourcesController : ControllerBase
    {
        private readonly FundingSourceService _service;

        public FundingSourcesController(FundingSourceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<FundingSource>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FundingSource>> GetById(int id)
        {
            var funding = await _service.GetByIdAsync(id);
            if (funding == null) return NotFound();
            return Ok(funding);
        }

        [HttpPost]
        public async Task<ActionResult<FundingSource>> Create(FundingSourceDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<FundingSource>> Update(int id, FundingSourceDto dto)
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

