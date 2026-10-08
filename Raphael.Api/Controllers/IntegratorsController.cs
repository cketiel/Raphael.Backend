using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;

using Raphael.Api.Attributes;
namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The integrators we actually work with: the external systems that create trips against
    /// our API with a key of their own.
    /// </summary>
    /// <remarks>
    /// ⚠️ Administrators only, added in RE-027. Before that this controller asked only for a
    /// valid token, and <c>GET</c> hands out every integrator's <c>ApiKey</c> in the clear —
    /// which means any authenticated user at all, a driver included, could read the keys that
    /// let a caller create trips in our system. Role 1 is Administrator, issued at login as the
    /// numeric RoleId.
    ///
    /// <para>
    /// The entities that could become integrators, but are not yet, live in
    /// <c>api/admin/catalog/integrators</c>.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "1")]
    // Office administration: role 1 is also a clinic's admin, who must not reach it.
    [NotForClinicUsers]
    public class IntegratorsController : ControllerBase
    {
        private readonly IIntegratorService _service;

        public IntegratorsController(IIntegratorService service)
        {
            _service = service;
        }

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpPost] public async Task<IActionResult> Create(IntegratorDto dto) => Ok(await _service.CreateAsync(dto));

        [HttpPut("{id}")] public async Task<IActionResult> Update(int id, IntegratorDto dto) => Ok(await _service.UpdateAsync(id, dto));

        [HttpDelete("{id}")] public async Task<IActionResult> Delete(int id) => Ok(await _service.DeleteAsync(id));
    }
}
