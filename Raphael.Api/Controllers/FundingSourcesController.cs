using Raphael.Api.Attributes;

using Raphael.Shared.Dtos;
using Raphael.Shared.Entities;
using Raphael.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The funding sources — the broker's book of clients.
    /// </summary>
    /// <remarks>
    /// A clinic (an integrator user) sees only the one funding source its integrator is linked
    /// to: the whole list is the broker's client portfolio. Changing the catalogue is for
    /// administrators (role 1); any other session used to be able to create, edit or delete it.
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class FundingSourcesController : ControllerBase
    {
        private readonly FundingSourceService _service;
        private readonly ICurrentUserService _currentUser;
        private readonly IIntegratorService _integrators;

        public FundingSourcesController(
            FundingSourceService service,
            ICurrentUserService currentUser,
            IIntegratorService integrators)
        {
            _service = service;
            _currentUser = currentUser;
            _integrators = integrators;
        }

        /// <summary>The funding source of the calling clinic's integrator, or null for anyone else.</summary>
        private async Task<FundingSource?> ClinicFundingSourceAsync() =>
            await _integrators.GetFundingSourceByIntegratorIdAsync(_currentUser.IntegratorId);

        /*[HttpGet]
        public async Task<ActionResult<List<FundingSource>>> GetAll()
        {
            var list = await _service.GetAllAsync();
            return Ok(list);
        }*/

        [HttpGet]
        public async Task<ActionResult<List<FundingSource>>> GetAll([FromQuery] bool includeInactive = false)
        {
            if (_currentUser.IntegratorId != null)
            {
                // Still a list, so the old Booking Web, which builds its report from it, keeps working.
                var own = await ClinicFundingSourceAsync();
                return Ok(own == null ? new List<FundingSource>() : new List<FundingSource> { own });
            }

            var list = await _service.GetAllAsync(includeInactive);
            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<FundingSource>> GetById(int id)
        {
            if (_currentUser.IntegratorId != null)
            {
                var own = await ClinicFundingSourceAsync();
                return own != null && own.Id == id ? Ok(own) : NotFound();
            }

            var funding = await _service.GetByIdAsync(id);
            if (funding == null) return NotFound();
            return Ok(funding);
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        [NotForClinicUsers]
        public async Task<ActionResult<FundingSource>> Create(FundingSourceDto dto)
        {
            var created = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "1")]
        [NotForClinicUsers]
        public async Task<ActionResult<FundingSource>> Update(int id, FundingSourceDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "1")]
        [NotForClinicUsers]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _service.DeleteAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }
    }
}

