using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Raphael.Shared.DbContexts;

namespace Raphael.Api.Observability
{
    /// <summary>
    /// Readiness probe: asks the database whether it is reachable.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately NOT part of <c>/health</c>. App Service restarts an instance whose health
    /// check stops answering 2xx, and restarting this API does nothing for a database that is
    /// unreachable — it drops every open SignalR connection on the dispatch board and loses the
    /// warm route cache, turning a transient SQL blip into a longer outage than the blip.
    /// The SQL firewall is opened to the App Service outbound addresses, which change when the
    /// plan is scaled, so "the database stopped answering" is a thing that happens for reasons
    /// a restart cannot fix.
    /// </para>
    /// <para>
    /// It is exposed on <c>/health/ready</c>, behind authentication, for a human deciding
    /// whether the app is merely up or actually able to work.
    /// </para>
    /// </remarks>
    public sealed class DatabaseHealthCheck : IHealthCheck
    {
        /// <summary>Tag that puts a check on <c>/health/ready</c> and keeps it off <c>/health</c>.</summary>
        public const string ReadyTag = "ready";

        private readonly RaphaelContext _context;

        /// <summary>Creates the check over the request's own context.</summary>
        public DatabaseHealthCheck(RaphaelContext context) => _context = context;

        /// <inheritdoc />
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                return await _context.Database.CanConnectAsync(cancellationToken)
                    ? HealthCheckResult.Healthy("The database answered.")
                    : HealthCheckResult.Unhealthy("The database did not answer.");
            }
            catch (Exception ex)
            {
                // The response body is the status word and nothing else: a connection failure
                // names the server, and that does not belong in an HTTP response.
                return HealthCheckResult.Unhealthy("The database did not answer.", ex);
            }
        }
    }
}
