using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Catalog;
using Raphael.Shared.DTOs.Catalog;

using Raphael.Api.Attributes;
namespace Raphael.Api.Controllers.Admin
{
    /// <summary>The fixed lists the catalog screens are built out of.</summary>
    /// <remarks>⚠️ Administrators only, like the rest of the catalog.</remarks>
    [ApiController]
    [Route("api/admin/catalog")]
    [Authorize(Roles = "1")]
    // Office administration: role 1 is also a clinic's admin, who must not reach it.
    [NotForClinicUsers]
    public sealed class CatalogLookupsController : ControllerBase
    {
        private readonly ICatalogLookupService _lookups;

        public CatalogLookupsController(ICatalogLookupService lookups)
        {
            _lookups = lookups;
        }

        /// <summary>The groups of the integrator catalog, each with how many rows it holds.</summary>
        [HttpGet("integrator-categories")]
        public async Task<ActionResult<IReadOnlyList<CatalogCategoryDto>>> IntegratorCategories(
            CancellationToken cancellationToken)
        {
            return Ok(await _lookups.GetIntegratorCategoriesAsync(cancellationToken));
        }

        /// <summary>The groups of the provider catalog, each with how many rows it holds.</summary>
        [HttpGet("provider-categories")]
        public async Task<ActionResult<IReadOnlyList<CatalogCategoryDto>>> ProviderCategories(
            CancellationToken cancellationToken)
        {
            return Ok(await _lookups.GetProviderCategoriesAsync(cancellationToken));
        }

        /// <summary>
        /// The counties. <paramref name="inUseOnly"/> narrows them to the ones the catalog
        /// actually uses, which is what a filter dropdown wants.
        /// </summary>
        [HttpGet("counties")]
        public async Task<ActionResult<IReadOnlyList<CountyDto>>> Counties(
            [FromQuery] string? state,
            [FromQuery] bool inUseOnly,
            CancellationToken cancellationToken)
        {
            return Ok(await _lookups.GetCountiesAsync(state, inUseOnly, cancellationToken));
        }
    }
}
