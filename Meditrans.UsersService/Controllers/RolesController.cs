using Microsoft.AspNetCore.Mvc;
using Meditrans.Shared.Entities;
using Meditrans.UsersService.Services;

namespace Meditrans.UsersService.Controllers
{
    [ApiController]
    [Route("api/roles")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _service;

        public RolesController(IRoleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var role = await _service.GetByIdAsync(id);
            return role == null ? NotFound() : Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> Create(Role role) => Ok(await _service.CreateAsync(role));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Role role)
        {
            var updated = await _service.UpdateAsync(id, role);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok() : NotFound();
        }
    }
}
