using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Dtos;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FundingSourceBillingItemController : ControllerBase
    {
        private readonly IFundingSourceBillingItemService _service;

        public FundingSourceBillingItemController(IFundingSourceBillingItemService service)
        {
            _service = service;
        }

        [HttpGet("ByFundingSource/{fundingSourceId}")]
        public async Task<ActionResult<List<FundingSourceBillingItemGetDto>>> GetByFundingSource(int fundingSourceId, [FromQuery] bool includeExpired = false)
        {
            var list = await _service.GetByFundingSourceIdAsync(fundingSourceId, includeExpired);
            return Ok(list);
        }

        [HttpGet]
        public async Task<ActionResult<List<FundingSourceBillingItemGetDto>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FundingSourceBillingItem>> GetById(int id)
        {
            var funding = await _service.GetByIdAsync(id);
            if (funding == null) return NotFound();
            return Ok(funding);
        }

        [HttpPost]
        public async Task<ActionResult<FundingSourceBillingItem>> Create([FromBody] FundingSourceBillingItemDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] FundingSourceBillingItemDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}

