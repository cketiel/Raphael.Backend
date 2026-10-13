using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Entities;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController : ControllerBase
    {
        /// <summary>
        /// RoleId of an administrator. Reading roles stays open (the user forms list them);
        /// creating, renaming or deleting one is admin-only (B20).
        /// </summary>
        private const string AdminRoleId = "1";

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
        [Authorize(Roles = AdminRoleId)]
        public async Task<ActionResult<RoleDto>> Create(RoleDto roleDto)
        {
            var newRole = await _roleService.CreateAsync(roleDto);
            return CreatedAtAction(nameof(GetById), new { id = newRole.Id }, newRole);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = AdminRoleId)]
        public async Task<IActionResult> Update(int id, RoleDto roleDto)
        {
            //if (id != roleDto.Id) return BadRequest();

            var updated = await _roleService.UpdateAsync(id, roleDto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = AdminRoleId)]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _roleService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }
}

