using Raphael.Shared.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Keeps the tally of what we ask Google and what the cache answers.
    /// </summary>
    /// <remarks>
    /// ⚠️ This is telemetry, and telemetry never fails a request. Every method here swallows its
    /// own errors: a dispatcher must not lose a route because a counter could not be written.
    /// </remarks>
    public interface IMapsUsageService
    {
        /// <summary>
        /// Adds to today's tally for one product.
        /// </summary>
        /// <param name="billed">
        /// True when the requests went to Google and cost money, false when our cache answered.
        /// </param>
        Task RecordAsync(MapsSku sku, bool billed, int count, CancellationToken cancellationToken);
    }
}
