using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Attributes;
using Raphael.Api.Services.Auth;
using Raphael.Api.Services.Places;
using Raphael.Shared.DTOs.Places;

namespace Raphael.Api.Controllers.Admin
{
    /// <summary>
    /// The one-off seed of places and notes from the trip history (RE-054).
    /// </summary>
    /// <remarks>
    /// ⚠️ Platform administrator only. Run it in DEV first, with <c>dryRun=true</c>, and in
    /// production only once the user has reviewed DEV (decided 2026-10-17). One month per call:
    /// the API's request timeout bounds how much one call can do. Running a month twice creates
    /// nothing new. Never calls Google.
    /// </remarks>
    [ApiController]
    [Route("api/admin/places")]
    [Authorize(Policy = AuthPolicies.PlatformAdmin)]
    [NotForClinicUsers]
    public sealed class PlacesSeedController : ControllerBase
    {
        private const int MaxDays = 62;

        private readonly ITripNotesService _notes;

        public PlacesSeedController(ITripNotesService notes)
        {
            _notes = notes;
        }

        /// <summary>Seeds places, entrances and notes to review from the trips between two dates.</summary>
        /// <param name="from">First trip date, inclusive.</param>
        /// <param name="to">Last trip date, inclusive. At most 62 days after <paramref name="from"/>.</param>
        /// <param name="dryRun">True (the default) writes nothing and reports what would be created.</param>
        [HttpPost("seed-from-history")]
        public async Task<ActionResult<PlacesSeedResultDto>> Seed([FromQuery] DateTime from, [FromQuery] DateTime to, [FromQuery] bool dryRun = true)
        {
            if (to.Date < from.Date) return BadRequest("The end date cannot be earlier than the start date.");
            if ((to.Date - from.Date).TotalDays > MaxDays) return BadRequest($"At most {MaxDays} days per call. Run it month by month.");

            return Ok(await _notes.SeedFromHistoryAsync(from, to, dryRun));
        }
    }
}
