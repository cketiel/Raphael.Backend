using Raphael.Api.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using System.Threading.Tasks;

using Raphael.Api.Attributes;
using Microsoft.EntityFrameworkCore;
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
        [AllowApps(ClientApps.Desktop, ClientApps.Driver)]
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

        /// <summary>
        /// The Providers Raphael's admin may give a trip to: those with at least one active user. A
        /// trip given to an account nobody can see leaves the patient without a ride
        /// (PROVIDER_ACCESS_MODEL.md §3.3). The update checks it again.
        /// </summary>
        [HttpGet("assignable")]
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [NotForClinicUsers]
        public async Task<IActionResult> GetAssignable(
            [FromServices] Raphael.Shared.DbContexts.RaphaelContext context,
            CancellationToken ct) =>
            Ok(await context.Providers.IgnoreQueryFilters().AsNoTracking()
                .Where(p => context.Users.IgnoreQueryFilters().Any(u => u.ProviderId == p.Id && u.IsActive))
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync(ct));

        /// <summary>The clinics that contracted this Provider (PROVIDER_ACCESS_MODEL.md §3.6). Office admins only.</summary>
        [HttpGet("{id:int}/clinics")]
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [NotForClinicUsers]
        [ProducesResponseType(typeof(List<Raphael.Shared.DTOs.Catalog.ProviderClinicRowDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetClinics(int id, [FromServices] Raphael.Api.Services.Catalog.IOfficeRelationsService relations, CancellationToken ct) =>
            Ok(await relations.GetProviderClinicsAsync(id, ct));


        [HttpPost]
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [NotForClinicUsers]
        public async Task<IActionResult> Create([FromForm] ProviderDto dto) 
        {
            var result = await _providerService.CreateAsync(dto);
            return Ok(result);
        }

        [HttpPut("{id}")]
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
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
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [NotForClinicUsers]
        public async Task<IActionResult> Delete(int id) => Ok(await _providerService.DeleteAsync(id));

        /// <summary>
        /// Makes a Provider a client company of the platform, or stops it being one (RE-049). Its
        /// users are held to their own data from their next sign-in or renewal.
        /// </summary>
        /// <remarks>
        /// Only Raphael's office: selling Raphael is the platform's act, never a client's. A Provider
        /// working outside Raphael (External) cannot be a tenant: it has no fleet here to isolate.
        /// </remarks>
        [HttpPut("{id:int}/tenant")]
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [NotForClinicUsers]
        public async Task<IActionResult> SetTenant(
            int id,
            [FromBody] ProviderTenantRequest request,
            [FromServices] Raphael.Shared.DbContexts.RaphaelContext context)
        {
            var provider = await context.Providers.FindAsync(id);
            if (provider == null) return NotFound();
            if (request.IsTenant && provider.WorkMode == Raphael.Shared.Entities.ProviderWorkMode.External)
                return BadRequest(new { code = "external_provider", message = "A Provider working outside Raphael cannot be a tenant." });

            provider.IsTenant = request.IsTenant;
            await context.SaveChangesAsync();
            return Ok(new { provider.Id, provider.IsTenant });
        }

        /// <summary>Body of <c>PUT api/Providers/{id}/tenant</c>.</summary>
        public sealed record ProviderTenantRequest(bool IsTenant);

        /// <summary>
        /// Moves a Provider between working in Raphael and working outside it (PROVIDER_ACCESS_MODEL.md
        /// §3.2). Its users follow from their next sign-in: External only enters the Booking portal.
        /// </summary>
        /// <remarks>
        /// Going External also stops it being a tenant: a Provider with no fleet here has nothing to
        /// isolate. Going to Raphael keeps its users as they are, including any a clinic created
        /// (user decision, 2026-10-16; the risk is written in §3.2).
        /// </remarks>
        [HttpPut("{id:int}/work-mode")]
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [NotForClinicUsers]
        public async Task<IActionResult> SetWorkMode(
            int id,
            [FromBody] ProviderWorkModeRequest request,
            [FromServices] Raphael.Shared.DbContexts.RaphaelContext context)
        {
            if (!Enum.IsDefined(request.WorkMode)) return BadRequest(new { code = "invalid_work_mode" });

            var provider = await context.Providers.FindAsync(id);
            if (provider == null) return NotFound();

            provider.WorkMode = request.WorkMode;
            if (request.WorkMode == Raphael.Shared.Entities.ProviderWorkMode.External)
                provider.IsTenant = false;
            await context.SaveChangesAsync();
            return Ok(new { provider.Id, provider.WorkMode, provider.IsTenant });
        }

        /// <summary>Body of <c>PUT api/Providers/{id}/work-mode</c>.</summary>
        public sealed record ProviderWorkModeRequest(Raphael.Shared.Entities.ProviderWorkMode WorkMode);

        /// <summary>"" when the form carries the field, null when it does not.</summary>
        private string? Carried(string field) =>
            Request.HasFormContentType && Request.Form.ContainsKey(field) ? string.Empty : null;
    }
}