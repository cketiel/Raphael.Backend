using Raphael.Shared.Entities;
using Raphael.Shared.Dtos;
using Raphael.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SpaceTypesController : ControllerBase
    {
        private readonly SpaceTypeService _service;

        public SpaceTypesController(SpaceTypeService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<SpaceType>>> GetAll()
        {
            return await _service.GetAllAsync();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<SpaceType>> GetById(int id)
        {
            var spaceType = await _service.GetByIdAsync(id);
            if (spaceType == null) return NotFound();
            return spaceType;
        }

        [HttpPost]
        public async Task<ActionResult<SpaceType>> Create(SpaceTypeDto dto)
        {          
            try
            {
                var created = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (DbUpdateException ex)
            {
                // Check if the error is due to duplication of the "Name" field
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 2601)
                {                  
                    return Conflict("The name already exists.");
                }
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            if (!deleted) return NotFound();
            return NoContent();
        }

        [HttpGet("ByName/{name}")]
        public async Task<ActionResult<SpaceType>> GetByName(string name)
        {
            var spaceType = await _service.GetByNameAsync(name);
            if (spaceType == null) return NotFound();
            return spaceType;
        }
    }
}

