using Microsoft.Extensions.Options;

namespace Raphael.Api.Versioning
{
    /// <summary>
    /// Reads which application and version is calling, tells it when it is older than this build
    /// expects, and leaves the answer where telemetry can pick it up.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It exists to answer one support question that used to have no answer: a ticket says the
    /// Schedule tab is wrong, and nobody can say which Raphael.Desktop the person was running,
    /// against which build of the API. Both halves of that are now stamped on the request in
    /// Application Insights (see <c>ClientVersionTelemetryInitializer</c>), so the question is a
    /// query and not an interrogation.
    /// </para>
    /// <para>
    /// ⚠️ <b>It never blocks and never changes a status code.</b> An outdated client is marked
    /// and served. The alternative — refusing the call — turns a cosmetic mismatch into a
    /// driver stranded at a dialysis centre with an app that will not load his route, and the
    /// person who can update it is three hours away. Marking is enough: the applications show
    /// the notice themselves.
    /// </para>
    /// <para>
    /// The headers are written from <c>OnStarting</c> because by the time a controller has
    /// finished, the response may already be on the wire. Response compression and the security
    /// headers sit further down the same pipeline for the same reason.
    /// </para>
    /// </remarks>
    public sealed class ClientCompatibilityMiddleware
    {
        /// <summary>
        /// Key under which the calling application's name is left in <c>HttpContext.Items</c>.
        /// </summary>
        public const string AppItemKey = "Raphael.ClientApp";

        /// <summary>
        /// Key under which the calling application's version is left in <c>HttpContext.Items</c>.
        /// </summary>
        public const string VersionItemKey = "Raphael.ClientVersion";

        private const string OutdatedStatus = "outdated";

        private readonly RequestDelegate _next;
        private readonly IOptionsMonitor<ClientCompatibilityOptions> _options;

        public ClientCompatibilityMiddleware(
            RequestDelegate next,
            IOptionsMonitor<ClientCompatibilityOptions> options)
        {
            _next = next;
            _options = options;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var app = Trim(context.Request.Headers[ClientVersionHeaders.App]);
            var version = Trim(context.Request.Headers[ClientVersionHeaders.Version]);

            // Nothing declared: the overwhelming majority of calls on the day this shipped, and
            // every call from a browser page or an integrator. Cost of the feature for them is
            // two dictionary lookups.
            if (app is null)
                return _next(context);

            context.Items[AppItemKey] = app;

            if (version is not null)
                context.Items[VersionItemKey] = version;

            var minimum = Minimum(app);

            if (version is not null && minimum is not null && IsOlder(version, minimum))
            {
                context.Response.OnStarting(static state =>
                {
                    var (response, floor) = ((HttpResponse, string))state;

                    response.Headers[ClientVersionHeaders.Status] = OutdatedStatus;
                    response.Headers[ClientVersionHeaders.Minimum] = floor;

                    return Task.CompletedTask;
                }, (context.Response, minimum));
            }

            return _next(context);
        }

        private string? Minimum(string app) =>
            _options.CurrentValue.MinimumVersions.TryGetValue(app, out var minimum)
            && !string.IsNullOrWhiteSpace(minimum)
                ? minimum
                : null;

        private static string? Trim(string? value)
        {
            value = value?.Trim();

            // A header long enough to be a payload is not a version. Whatever it is, it is not
            // going into telemetry or into a response header.
            return string.IsNullOrEmpty(value) || value.Length > 32 ? null : value;
        }

        /// <summary>
        /// Whether <paramref name="version"/> is older than <paramref name="minimum"/>, for
        /// version strings of the shape the three applications actually produce.
        /// </summary>
        /// <remarks>
        /// A pre-release suffix is cut before comparing and then used to break a tie, which is
        /// what SemVer says: <c>0.1.0-preview.3</c> comes before <c>0.1.0</c>. Anything that
        /// does not parse is treated as current — a client that reports its version oddly is
        /// not evidence that it is out of date.
        /// </remarks>
        private static bool IsOlder(string version, string minimum)
        {
            var (clientCore, clientIsPreRelease) = Split(version);
            var (floorCore, floorIsPreRelease) = Split(minimum);

            if (!Version.TryParse(clientCore, out var client)
                || !Version.TryParse(floorCore, out var floor))
            {
                return false;
            }

            var comparison = client.CompareTo(floor);

            if (comparison != 0)
                return comparison < 0;

            return clientIsPreRelease && !floorIsPreRelease;
        }

        private static (string Core, bool IsPreRelease) Split(string version)
        {
            var cut = version.IndexOfAny(new[] { '-', '+' });

            return cut < 0
                ? (version, false)
                : (version[..cut], version[cut] == '-');
        }
    }
}
