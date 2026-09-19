using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Raphael.Api.Observability
{
    /// <summary>
    /// Keeps the App Service probes out of Application Insights while they are succeeding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// App Service calls the health check every minute, for ever, and asks for the root every few
    /// minutes to keep the instance warm. That is about 50,000 requests a month which are not
    /// traffic, on an account that is billed by the volume it ingests, and they drown the real
    /// request list: a chart of "requests over time" that is 99% probe tells you nothing about
    /// the dispatch office.
    /// </para>
    /// <para>
    /// ⚠️ Only the ones that SUCCEED are dropped. A probe that fails is the single most
    /// interesting event this application can produce — it is App Service deciding the instance
    /// is sick — and it stays. Dropping health checks wholesale is how a service quietly goes
    /// unhealthy with nothing in the telemetry to show for it.
    /// </para>
    /// <para>
    /// A processor and not an initializer, and that distinction is the whole reason this class
    /// exists: an initializer can only change an item on its way through. Discarding one is
    /// something only a processor can do.
    /// </para>
    /// </remarks>
    public sealed class HealthProbeTelemetryProcessor : ITelemetryProcessor
    {
        /// <summary>
        /// The liveness endpoint. Shared with the code that maps it, so the two cannot drift:
        /// a processor filtering a path the application no longer serves filters nothing, and
        /// says nothing about it either.
        /// </summary>
        public const string LivenessPath = "/health";

        /// <summary>
        /// The root, which App Service pings to keep a warm instance. Filtered for the same
        /// reason as the liveness probe and under the same condition — only while it succeeds.
        /// </summary>
        /// <remarks>
        /// ⚠️ This filter is only correct because the application serves a root. Take that
        /// endpoint away and every warm-up ping becomes a 404, which fails, which means the
        /// filter stops dropping it — the noise comes back rather than being hidden. That is the
        /// behaviour we want: the day the root stops answering, the telemetry says so.
        /// </remarks>
        public const string WarmupPath = "/";

        private readonly ITelemetryProcessor _next;

        /// <summary>Processors are a chain; each one is handed the next.</summary>
        public HealthProbeTelemetryProcessor(ITelemetryProcessor next) => _next = next;

        /// <inheritdoc />
        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry request && IsSuccessfulProbe(request))
            {
                return;
            }

            _next.Process(item);
        }

        private static bool IsSuccessfulProbe(RequestTelemetry request)
        {
            if (request.Success == false)
            {
                return false;
            }

            var path = request.Url?.AbsolutePath;

            // Exact match. /health/ready is a different endpoint, asked for by a person who
            // wants an answer about the database, and there are a handful of those a month.
            return string.Equals(path, LivenessPath, StringComparison.OrdinalIgnoreCase)
                || string.Equals(path, WarmupPath, StringComparison.Ordinal);
        }
    }
}
