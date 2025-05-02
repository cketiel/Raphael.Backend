using Meditrans.Shared.Entities;
using Meditrans.Shared.Dtos;
using Meditrans.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Meditrans.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _service;

        public CustomersController(CustomerService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<Customer>>> GetAll()
        {
            return await _service.GetAllAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Customer>> GetById(int id)
        {
            var customer = await _service.GetByIdAsync(id);
            if (customer == null) return NotFound();
            return customer;
        }

        [HttpPost]
        public async Task<ActionResult<Customer>> Create(CustomerDto dto)
        {
            try
            {               
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {               
                //Console.WriteLine(ex.ToString());
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
               
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<Customer>> Update(int id, CustomerDto dto)
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
