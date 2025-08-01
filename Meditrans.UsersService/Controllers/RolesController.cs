using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Entities;
using Raphael.UsersService.Services;
using Raphael.UsersService.DTOs;

namespace Raphael.UsersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RoleDto>>> GetAll()
        {
            return Ok(await _roleService.GetAllAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RoleDto>> GetById(int id)
        {
            var role = await _roleService.GetByIdAsync(id);
            if (role == null) return NotFound();
            return Ok(role);
        }

        [HttpPost]
        public async Task<ActionResult<RoleDto>> Create(RoleDto roleDto)
        {
            var newRole = await _roleService.CreateAsync(roleDto);
            return CreatedAtAction(nameof(GetById), new { id = newRole.Id }, newRole);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, RoleDto roleDto)
        {
            //if (id != roleDto.Id) return BadRequest();

            var updated = await _roleService.UpdateAsync(id, roleDto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _roleService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}

