using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Raphael.Api.Versioning;

namespace Raphael.Api.Observability
{
    /// <summary>
    /// Stamps every piece of telemetry raised while serving a request with the application and
    /// version that made it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Application Insights already records which build of the API produced a trace — it reads
    /// the assembly version into <c>application_Version</c> on its own, which is the reason
    /// <c>Directory.Build.props</c> declares one. What it cannot know is who was on the other
    /// end. This supplies that half.
    /// </para>
    /// <para>
    /// With both, a support ticket stops being a conversation. "The import failed on Tuesday"
    /// becomes a query: exceptions in that window, grouped by <c>ClientApp</c> and
    /// <c>ClientVersion</c>, sliced by <c>application_Version</c>. Which is also how you find
    /// out that a fault only ever happens to one version of Raphael.Desktop.
    /// </para>
    /// <para>
    /// ⚠️ Two properties and nothing else. Telemetry leaves the building and is retained for
    /// months, so what goes in it is chosen, never copied wholesale from the request — see
    /// <see cref="QueryStringScrubbingInitializer"/> for what happens when it is not. Neither
    /// value identifies a person.
    /// </para>
    /// </remarks>
    public sealed class ClientVersionTelemetryInitializer : ITelemetryInitializer
    {
        private const string AppProperty = "ClientApp";
        private const string VersionProperty = "ClientVersion";

        private readonly IHttpContextAccessor _accessor;

        public ClientVersionTelemetryInitializer(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            if (telemetry is not ISupportProperties supportsProperties)
                return;

            var items = _accessor.HttpContext?.Items;

            if (items is null)
                return;

            Copy(items, ClientCompatibilityMiddleware.AppItemKey, supportsProperties, AppProperty);
            Copy(items, ClientCompatibilityMiddleware.VersionItemKey, supportsProperties, VersionProperty);
        }

        private static void Copy(
            IDictionary<object, object?> items,
            string itemKey,
            ISupportProperties telemetry,
            string propertyName)
        {
            if (items.TryGetValue(itemKey, out var value)
                && value is string text
                && !telemetry.Properties.ContainsKey(propertyName))
            {
                telemetry.Properties[propertyName] = text;
            }
        }
    }
}
