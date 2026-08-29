using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Routing;
using Raphael.Shared.DTOs.Routing;

namespace Raphael.Api.Controllers.Admin
{
    /// <summary>
    /// What Google Maps is costing, and what the cache is saving.
    /// </summary>
    /// <remarks>
    /// ⚠️ Administrators only, the same check as the settings panel: role 1. These figures are
    /// what an administrator sets Cloud Console quotas against, and a quota set from a partial
    /// view of the traffic is worse than no quota at all.
    /// </remarks>
    [ApiController]
    [Route("api/admin/maps-usage")]
    [Authorize(Roles = "1")]
    public sealed class MapsUsageController : ControllerBase
    {
        /// <summary>
        /// How far back a single request may ask. Three years of daily rows is a few thousand,
        /// but an unbounded range is an invitation to scan the table for nothing.
        /// </summary>
        private const int MaxRangeDays = 1100;

        private readonly IMapsUsageReportService _report;

        public MapsUsageController(IMapsUsageReportService report)
        {
            _report = report;
        }

        /// <summary>
        /// The period's headline figures: spent, saved, cache hit rate, and a per-product split.
        /// </summary>
        /// <remarks>
        /// The period is the administrator's simulated billing cycle. Google's volume bands are
        /// monthly, so a range that is not a calendar month gives an estimate of a volume Google
        /// never priced — the panel says so where the reader can see it.
        /// </remarks>
        [HttpGet("summary")]
        public async Task<ActionResult<MapsUsageSummaryDto>> GetSummary(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            if (!TryValidate(from, to, out var error)) return BadRequest(error);

            return Ok(await _report.GetSummaryAsync(from, to, cancellationToken));
        }

        /// <summary>One point per day and product, for the charts.</summary>
        [HttpGet("daily")]
        public async Task<ActionResult<IReadOnlyList<MapsUsagePointDto>>> GetDaily(
            [FromQuery] DateTime from,
            [FromQuery] DateTime to,
            CancellationToken cancellationToken)
        {
            if (!TryValidate(from, to, out var error)) return BadRequest(error);

            return Ok(await _report.GetDailyAsync(from, to, cancellationToken));
        }

        /// <summary>Everything ever counted, with no date filter.</summary>
        [HttpGet("totals")]
        public async Task<ActionResult<MapsUsageTotalsDto>> GetTotals(
            CancellationToken cancellationToken)
        {
            return Ok(await _report.GetTotalsAsync(cancellationToken));
        }

        /// <summary>Google's volume bands as configured.</summary>
        [HttpGet("pricing")]
        public async Task<ActionResult<IReadOnlyList<MapsPricingTierDto>>> GetPricing(
            CancellationToken cancellationToken)
        {
            return Ok(await _report.GetPricingAsync(cancellationToken));
        }

        private static bool TryValidate(DateTime from, DateTime to, out string error)
        {
            error = string.Empty;

            if (from > to)
            {
                error = "The start of the period must not be after its end.";
                return false;
            }

            if ((to - from).TotalDays > MaxRangeDays)
            {
                error = $"The period must not span more than {MaxRangeDays} days.";
                return false;
            }

            return true;
        }
    }
}
