using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace Raphael.Api.Observability
{
    /// <summary>
    /// Cuts every URL that reaches Application Insights at the '?'.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The constitution forbids patient data in logs and URLs, and telemetry is a log that
    /// leaves the building: once a query string is in Application Insights it is queryable,
    /// exportable and retained for months.
    /// </para>
    /// <para>
    /// Two query strings in this API are known to carry something that must not be stored,
    /// and they are the reason this class is not optional:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     Incoming requests to the SignalR hubs carry <c>?access_token=</c>. That is a bearer
    ///     token good for ten hours, written into the request URL because the transport cannot
    ///     put it in a header.
    ///   </item>
    ///   <item>
    ///     Outgoing calls to Google carry the pickup address and the Maps API key in the query.
    ///     Application Insights records an HTTP dependency's full URL in <c>Data</c>, so a
    ///     geocoding call publishes both the patient's address and a billable credential.
    ///   </item>
    /// </list>
    /// <para>
    /// The whole query is removed rather than a list of parameter names redacted. A list has to
    /// be kept in step with every new parameter anyone adds, and the day it falls behind nothing
    /// says so: the data is simply in the telemetry. Cutting at the '?' cannot fall behind.
    /// What is lost is the filter arguments when diagnosing, which the operation name and this
    /// API's own logs still give.
    /// </para>
    /// </remarks>
    public sealed class QueryStringScrubbingInitializer : ITelemetryInitializer
    {
        /// <inheritdoc />
        public void Initialize(ITelemetry telemetry)
        {
            switch (telemetry)
            {
                // The URL of a request this API served.
                case RequestTelemetry request when request.Url is not null:
                    request.Url = Trim(request.Url);
                    break;

                // The URL of a call this API made. For an HTTP dependency `Data` is the full
                // target URL; for a SQL dependency it is the command text, which is not an
                // absolute URI and is therefore left untouched.
                case DependencyTelemetry dependency:
                    dependency.Data = TrimAbsoluteUrl(dependency.Data);
                    break;
            }
        }

        private static Uri Trim(Uri url) =>
            string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment)
                ? url
                : new Uri(url.GetLeftPart(UriPartial.Path));

        private static string? TrimAbsoluteUrl(string? value) =>
            !string.IsNullOrEmpty(value) && Uri.TryCreate(value, UriKind.Absolute, out var url)
                ? url.GetLeftPart(UriPartial.Path)
                : value;
    }
}
