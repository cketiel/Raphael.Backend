using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Routing;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Routing;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// Travel times, distances and coordinates for the applications.
    /// </summary>
    /// <remarks>
    /// Desktop and Driver used to call Google directly, each with the key on the machine — in a
    /// versioned <c>appsettings.json</c> in one case and inside a distributed APK in the other —
    /// and neither remembered anything from one call to the next. Everything now comes through
    /// here, which is what lets one dispatcher's answer serve the next dispatcher and the driver
    /// on the same route.
    ///
    /// <para>
    /// ⚠️ Requests here cost money when they miss the cache. Ask for the legs a screen needs in
    /// one batch, and do not ask again for a leg whose two endpoints have not moved.
    /// </para>
    /// </remarks>
    [ApiController]
    [Route("api/routing")]
    [Authorize]
    public class RoutingController : ControllerBase
    {
        private const int MaxLegsPerRequest = 100;
        private const int MaxAddressesPerRequest = 500;

        private readonly IRoutingService _routing;
        private readonly IMapsUsageService _usage;

        public RoutingController(IRoutingService routing, IMapsUsageService usage)
        {
            _routing = routing;
            _usage = usage;
        }

        /// <summary>
        /// Prices a batch of legs: one answer per leg, in the order asked.
        /// </summary>
        /// <remarks>
        /// A leg that could not be priced comes back with status <c>Unavailable</c> and the batch
        /// still succeeds. The caller must keep whatever value it already had for that leg —
        /// writing a zero there hands a driver an arrival time of "now".
        /// </remarks>
        [HttpPost("legs")]
        public async Task<ActionResult<RouteLegsResponseDto>> GetLegs(
            [FromBody] RouteLegsRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request?.Legs is null)
            {
                return BadRequest("A list of legs is required.");
            }

            if (request.Legs.Count > MaxLegsPerRequest)
            {
                // A ceiling, not a policy. Nothing in this system routes a hundred legs in one
                // screen, so a request that size is a loop that got away from somebody — and it
                // would be billed before anyone noticed.
                return BadRequest($"At most {MaxLegsPerRequest} legs can be priced in one request.");
            }

            return Ok(await _routing.GetLegsAsync(request, cancellationToken));
        }

        /// <summary>Resolves one address to coordinates.</summary>
        [HttpPost("geocode")]
        public async Task<ActionResult<GeocodeResultDto>> Geocode(
            [FromBody] GeocodeRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null) return BadRequest("An address is required.");

            return Ok(await _routing.GeocodeAsync(request, cancellationToken));
        }

        /// <summary>
        /// Resolves many addresses at once, paying only for the ones nobody has resolved before.
        /// </summary>
        /// <remarks>
        /// This is what the CSV import should call. Repeats inside the batch are resolved once:
        /// a day's import names the same dozen clinics on nearly every row.
        /// </remarks>
        [HttpPost("geocode/batch")]
        public async Task<ActionResult<GeocodeBatchResponseDto>> GeocodeBatch(
            [FromBody] GeocodeBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request?.Addresses is null) return BadRequest("A list of addresses is required.");

            if (request.Addresses.Count > MaxAddressesPerRequest)
            {
                return BadRequest($"At most {MaxAddressesPerRequest} addresses can be resolved in one request.");
            }

            return Ok(await _routing.GeocodeBatchAsync(request, cancellationToken));
        }

        /// <summary>The town a point sits in.</summary>
        [HttpPost("reverse-geocode")]
        public async Task<ActionResult<ReverseGeocodeResultDto>> ReverseGeocode(
            [FromBody] ReverseGeocodeRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request is null) return BadRequest("A coordinate is required.");

            return Ok(await _routing.ReverseGeocodeCityAsync(request, cancellationToken));
        }

        /// <summary>
        /// Records Google calls a client made on its own, so they reach the usage panel.
        /// </summary>
        /// <remarks>
        /// The map pages carry the browser key and talk to Google directly — a map load, an
        /// address autocomplete, a geocode when a pin is dragged. This server never sees them,
        /// and without this endpoint the panel would be blind to a third of the invoice.
        ///
        /// <para>
        /// Any signed-in client may report. This writes nothing but counters, and the worst a
        /// wrong report can do is make an estimate wrong — which is why the panel marks these
        /// SKUs as reported rather than measured. Unknown SKU names are ignored rather than
        /// refused: a client one version ahead should not get an error for it.
        /// </para>
        /// </remarks>
        [HttpPost("usage")]
        public async Task<IActionResult> ReportUsage(
            [FromBody] MapsUsageReportDto request,
            CancellationToken cancellationToken)
        {
            if (request?.Items is null || request.Items.Count == 0) return NoContent();

            foreach (var item in request.Items)
            {
                if (!Enum.TryParse<MapsSku>(item.Sku, ignoreCase: true, out var sku)) continue;

                // Only what a browser can actually buy. A client claiming to have made Routes
                // calls is either confused or lying, and either way those are counted here.
                if (!MapsPricingCalculator.IsReportedByClient(sku)) continue;

                if (item.Count is <= 0 or > 1000) continue;

                await _usage.RecordAsync(sku, billed: true, item.Count, cancellationToken);
            }

            return NoContent();
        }
    }
}
