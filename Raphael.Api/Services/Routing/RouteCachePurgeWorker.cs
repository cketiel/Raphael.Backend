using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Deletes cached Google content once it reaches thirty days.
    /// </summary>
    /// <remarks>
    /// ⚠️ This worker is a term of the licence, not an optimisation. Google's terms allow route
    /// and geocoding content to be cached temporarily and require it to be deleted after 30
    /// consecutive days. Reading past the cutoff is already prevented in <c>RoutingService</c>;
    /// this is what makes the row actually go away. If this worker is ever removed, the cache
    /// stops being a cache and becomes a copy of Google's database.
    ///
    /// <para>
    /// <c>ObservedLegTimes</c> is deliberately untouched. Those rows are our own vehicles'
    /// measurements, nothing in Google's terms reaches them, and they are what the automatic
    /// router will learn from.
    /// </para>
    ///
    /// <para>
    /// Runs on an interval rather than at a fixed hour, like the notification retention worker:
    /// a pass that lands a few hours late costs nothing, and starting on boot means a server that
    /// spent the weekend down cleans up as soon as it comes back.
    /// </para>
    /// </remarks>
    public class RouteCachePurgeWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromHours(12);
        private static readonly TimeSpan StartupDelay = TimeSpan.FromMinutes(3);

        private readonly IServiceProvider _services;
        private readonly ILogger<RouteCachePurgeWorker> _logger;

        public RouteCachePurgeWorker(
            IServiceProvider services,
            ILogger<RouteCachePurgeWorker> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(StartupDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PurgeAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // A failed pass must not take the API down. The next one deletes everything
                    // this one left, and the reader already refuses to serve expired rows.
                    _logger.LogError(ex, "Routing cache purge failed.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private async Task PurgeAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();

            var context = scope.ServiceProvider.GetRequiredService<RaphaelContext>();

            var now = DateTime.UtcNow;
            var routeCutoff = now - RoutingService.CacheLifetime;
            var notFoundCutoff = now - RoutingService.NotFoundLifetime;

            var legs = await context.RouteLegCache
                .Where(c => c.FetchedAtUtc < routeCutoff)
                .ExecuteDeleteAsync(cancellationToken);

            var addresses = await context.GeocodeCache
                .Where(c => c.Status == GeocodeStatus.Ok
                    ? c.FetchedAtUtc < routeCutoff
                    : c.FetchedAtUtc < notFoundCutoff)
                .ExecuteDeleteAsync(cancellationToken);

            if (legs > 0 || addresses > 0)
            {
                _logger.LogInformation(
                    "Routing cache purge deleted {Legs} legs and {Addresses} addresses past their 30-day limit.",
                    legs,
                    addresses);
            }
        }
    }
}
