using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using System.Threading.Tasks;

using Raphael.Api.Attributes;
namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The providers we actually work with: the companies whose vehicles carry out our trips.
    /// </summary>
    /// <remarks>
    /// ⚠️ Writing is administrators only, added in RE-027. Reading is not: the two
    /// <c>contact</c> endpoints serve the office contact card that the driver app shows, and
    /// <c>GetAll</c> feeds screens that are not restricted to role 1.
    ///
    /// <para>
    /// <c>PUT contact</c> is deliberately left as it was, and it is a known hole: a driver can
    /// change the office phone number. Closing it means also removing the editable field from
    /// the driver screen, which is a change to an app installed on 31 phones — it is written up
    /// in BACKLOG.md and does not belong in this slice.
    /// </para>
    ///
    /// <para>
    /// The entities that could become providers, but are not yet, live in
    /// <c>api/admin/catalog/providers</c>.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class ProvidersController : ControllerBase
    {
        private readonly IProviderService _providerService;

        public ProvidersController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        // GET: api/providers/contact
        [HttpGet("contact")]
        public async Task<ActionResult<ProviderDto>> GetContactProvider()
        {
            var provider = await _providerService.GetContactProviderAsync();
            if (provider == null)
            {
                
                return NotFound();
            }
            return Ok(provider);
        }

        // PUT: api/providers/contact
        [HttpPut("contact")]
        public async Task<IActionResult> UpdateContactProvider([FromBody] ProviderDto providerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _providerService.UpdateContactProviderAsync(providerDto);

            if (!success)
            {
                return NotFound("Contact provider not found and could not be updated.");
            }

            return NoContent(); // Hide the Text Customer and Send Dispatch Message actions
        }

        [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _providerService.GetAllAsync());

        [HttpPost]
        [Authorize(Roles = "1")]
        [NotForClinicUsers]
        public async Task<IActionResult> Create([FromForm] ProviderDto dto) 
        {
            var result = await _providerService.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "1")]
        [NotForClinicUsers]
        public async Task<IActionResult> Update(int id, [FromForm] ProviderDto dto)
        {
            // Form binding turns an empty field into null, which the service reads as "not sent,
            // keep it" (AccountIdentity.Sent). A field the form does carry, empty, is a clear.
            dto.TimeZoneId ??= Carried(nameof(ProviderDto.TimeZoneId));
            dto.Website ??= Carried(nameof(ProviderDto.Website));
            dto.ContactName ??= Carried(nameof(ProviderDto.ContactName));
            dto.Comments ??= Carried(nameof(ProviderDto.Comments));

            var success = await _providerService.UpdateAsync(id, dto);
            return success ? Ok() : NotFound();
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "1")]
        [NotForClinicUsers]
        public async Task<IActionResult> Delete(int id) => Ok(await _providerService.DeleteAsync(id));

        /// <summary>"" when the form carries the field, null when it does not.</summary>
        private string? Carried(string field) =>
            Request.HasFormContentType && Request.Form.ContainsKey(field) ? string.Empty : null;
    }
}