using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.Dtos;
using Raphael.Shared.Entities;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")] 
    public class BillingItemsController : ControllerBase
    {
        private readonly BillingItemService _service;

        public BillingItemsController(BillingItemService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<BillingItem>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillingItem>> GetById(int id)
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null)
            {
                return NotFound();
            }
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<BillingItem>> Create(BillingItemDto dto)
        {
            var createdItem = await _service.CreateAsync(dto);
            // We return 201 Created with the location of the new resource and the created object
            return CreatedAtAction(nameof(GetById), new { id = createdItem.Id }, createdItem);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<BillingItem>> Update(int id, BillingItemDto dto)
        {
            var updatedItem = await _service.UpdateAsync(id, dto);
            if (updatedItem == null)
            {
                return NotFound();
            }
            return Ok(updatedItem);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }
            // We return 204 No Content, which indicates that the operation was successful but there is nothing to return
            return NoContent();
        }
    }
}