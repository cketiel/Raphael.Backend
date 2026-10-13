using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;

using Raphael.Api.Attributes;
using Raphael.Api.Services.Auth;
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
    // Any admin manages its own clinics ("my integrators"): a client company's admin lists,
    // creates, edits and deletes only the ones it owns, because the tenant query filter hides the
    // rest and the owner is taken from the token (RE-049). Who works with whom (relations, catalog
    // link) is the platform's. Role 1 is also a clinic's admin, who must not reach it.
    [Authorize(Roles = "1")]
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

        // ---- Who works with whom (PROVIDER_ACCESS_MODEL.md §3.6): the office sees and corrects a
        // clinic's contracted Providers and its row in the catalog. Office admins only (class attributes).

        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [HttpGet("{id:int}/relations")]
        [ProducesResponseType(typeof(Raphael.Shared.DTOs.Catalog.IntegratorRelationsDto), StatusCodes.Status200OK)]
        public Task<IActionResult> GetRelations(int id, [FromServices] Raphael.Api.Services.Catalog.IOfficeRelationsService relations, CancellationToken ct) =>
            Relations(async () => await relations.GetIntegratorRelationsAsync(id, ct), Ok);

        /// <summary>Contracts a catalog Provider for this clinic, and gives it its account, as Booking does.</summary>
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [HttpPost("{id:int}/providers/{catalogProviderId:int}")]
        [ProducesResponseType(typeof(Raphael.Shared.DTOs.Catalog.IntegratorRelationsDto), StatusCodes.Status200OK)]
        public Task<IActionResult> AddProvider(int id, int catalogProviderId, [FromServices] Raphael.Api.Services.Catalog.IOfficeRelationsService relations, CancellationToken ct) =>
            Relations(async () => await relations.AddProviderAsync(id, catalogProviderId, ct), Ok);

        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [HttpDelete("{id:int}/providers/{catalogProviderId:int}")]
        [ProducesResponseType(typeof(Raphael.Shared.DTOs.Catalog.IntegratorRelationsDto), StatusCodes.Status200OK)]
        public Task<IActionResult> RemoveProvider(int id, int catalogProviderId, [FromServices] Raphael.Api.Services.Catalog.IOfficeRelationsService relations, CancellationToken ct) =>
            Relations(async () => await relations.RemoveProviderAsync(id, catalogProviderId, ct), Ok);

        /// <summary>Links the clinic to its row in the Integrator catalog (null unlinks): what an external Provider's "my clinics" is built on.</summary>
        [Authorize(Policy = AuthPolicies.PlatformAdmin)]
        [HttpPut("{id:int}/catalog-link")]
        [ProducesResponseType(typeof(Raphael.Shared.DTOs.Catalog.IntegratorRelationsDto), StatusCodes.Status200OK)]
        public Task<IActionResult> LinkCatalog(int id, [FromBody] Raphael.Shared.DTOs.Catalog.IntegratorCatalogLinkDto dto, [FromServices] Raphael.Api.Services.Catalog.IOfficeRelationsService relations, CancellationToken ct) =>
            Relations(async () => await relations.LinkCatalogAsync(id, dto?.CatalogIntegratorId, ct), Ok);

        private static async Task<IActionResult> Relations(Func<Task<object>> work, Func<object, IActionResult> ok)
        {
            try
            {
                return ok(await work());
            }
            catch (Raphael.Api.Services.Catalog.OfficeRelationsNotFoundException)
            {
                return new NotFoundResult();
            }
            catch (Raphael.Api.Services.Catalog.OfficeRelationsRuleException rule)
            {
                return new BadRequestObjectResult(rule.Message);
            }
        }

    }
}
