using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Deletes cached Google content once it outlives the retention the administrators set.
    /// </summary>
    /// <remarks>
    /// The retention is <c>Routing.CacheRetentionDays</c> in <c>SystemSettings</c> — the same
    /// value the reader in <c>RoutingService</c> honours, so what is readable and what still
    /// exists cannot drift apart. It defaults to a year, by business decision: Google's terms
    /// describe a 30-day window for cached content, and the operation — which has stored customer
    /// coordinates since it went live — chose to accept that posture and keep the dial in the
    /// administrators' hands. Turning it down to 30 takes effect on the next pass, no deployment.
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
            var settings = scope.ServiceProvider
                .GetRequiredService<Raphael.Api.Services.Admin.ISystemSettingService>();

            var retentionDays = await settings.GetIntAsync(
                Raphael.Api.Services.Admin.SystemSettingKeys.RoutingCacheRetentionDays,
                RoutingService.DefaultCacheRetentionDays,
                cancellationToken);

            if (retentionDays < 1) retentionDays = 1;

            var now = DateTime.UtcNow;
            var routeCutoff = now - TimeSpan.FromDays(retentionDays);
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
                    "Routing cache purge deleted {Legs} legs and {Addresses} addresses older than {Days} days.",
                    legs,
                    addresses,
                    retentionDays);
            }
        }
    }
}
