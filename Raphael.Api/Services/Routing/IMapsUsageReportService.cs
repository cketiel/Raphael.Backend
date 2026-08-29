using Raphael.Shared.DTOs.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Reads the usage tallies back: what we spent, what the cache saved, and how it is trending.
    /// </summary>
    public interface IMapsUsageReportService
    {
        /// <summary>Headline figures and a per-product breakdown for a billing period.</summary>
        Task<MapsUsageSummaryDto> GetSummaryAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken);

        /// <summary>One point per day and product, for the charts.</summary>
        Task<IReadOnlyList<MapsUsagePointDto>> GetDailyAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken);

        /// <summary>Everything ever counted, with no date filter.</summary>
        Task<MapsUsageTotalsDto> GetTotalsAsync(CancellationToken cancellationToken);

        /// <summary>Google's volume bands as configured.</summary>
        Task<IReadOnlyList<MapsPricingTierDto>> GetPricingAsync(CancellationToken cancellationToken);
    }
}
