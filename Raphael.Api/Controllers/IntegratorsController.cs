using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.DTOs;
using Raphael.Api.Services;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class IntegratorsController : ControllerBase
    {
        private readonly IIntegratorService _service;

        public IntegratorsController(IIntegratorService service) => _service = service;

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost] public async Task<IActionResult> Create(IntegratorDto dto) => Ok(await _service.CreateAsync(dto));

        [HttpPut("{id}")] public async Task<IActionResult> Update(int id, IntegratorDto dto) => Ok(await _service.UpdateAsync(id, dto));

        [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) => Ok(await _service.DeleteAsync(id));
    }
}