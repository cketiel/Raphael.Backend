using Raphael.Shared.Entities.Routing;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Raphael.Api.Services.Routing
{
    /// <summary>What Google charged us for: one leg's time and distance.</summary>
    public sealed class GoogleLegResult
    {
        public int DurationSeconds { get; init; }

        /// <summary>Null unless traffic was asked for and Google returned it.</summary>
        public int? DurationInTrafficSeconds { get; init; }

        public int DistanceMeters { get; init; }

        /// <summary>The road's shape, when it was asked for.</summary>
        public string? EncodedPolyline { get; init; }
    }

    /// <summary>
    /// The only place in the ecosystem that asks Google what a drive costs.
    /// </summary>
    /// <remarks>
    /// Routes API v2 rather than the legacy Directions endpoint the clients used to call. Three
    /// reasons, in order of weight: the field mask lets us ask for duration and distance and
    /// nothing else; the key travels in a header, so it never lands in a URL or a proxy log; and
    /// Directions is in legacy status. A traffic-aware request also returns the free-flow duration
    /// alongside the congested one, which is why one request now replaces the three the Desktop
    /// used to make for the same screen.
    ///
    /// <para>
    /// ⚠️ Every call through here is billable. Nothing in this class decides whether a call is
    /// needed — that is <c>RoutingService</c>'s job, and it should have exhausted the cache first.
    /// </para>
    /// </remarks>
    public class GoogleRoutesClient
    {
        private const string Endpoint = "https://routes.googleapis.com/directions/v2:computeRoutes";

        /// <summary>
        /// The only three fields scheduling pays attention to. A wider mask pulls turn-by-turn
        /// text nobody here reads, and some fields raise the SKU.
        /// </summary>
        private const string FieldMask = "routes.duration,routes.staticDuration,routes.distanceMeters";

        /// <summary>
        /// The same, plus the road's shape, for the screens that draw a map.
        /// </summary>
        /// <remarks>
        /// A plain encoded polyline is an Essentials field. What would raise the tier is asking
        /// for traffic *along* the polyline (<c>extraComputations: TRAFFIC_ON_POLYLINE</c>,
        /// <c>travelAdvisory.speedReadingIntervals</c>) — deliberately not requested here. Either
        /// way this mask is only used when a map is open, a handful of times a day.
        /// </remarks>
        private const string FieldMaskWithPolyline = FieldMask + ",routes.polyline.encodedPolyline";

        private readonly HttpClient _http;
        private readonly ILogger<GoogleRoutesClient> _logger;
        private readonly string? _apiKey;

        public GoogleRoutesClient(
            HttpClient http,
            IConfiguration configuration,
            ILogger<GoogleRoutesClient> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = configuration["GoogleMaps:ApiKey"];

            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        /// <summary>
        /// Prices one leg. Returns null when Google could not answer — the caller decides what a
        /// missing leg means, and it is never zero.
        /// </summary>
        /// <remarks>
        /// <paramref name="departureUtc"/> is sent only in <see cref="RoutingTrafficMode.Precision"/>:
        /// Google rejects a departure time on a traffic-unaware request.
        /// </remarks>
        public async Task<GoogleLegResult?> ComputeRouteAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng,
            RoutingTrafficMode mode,
            DateTime? departureUtc,
            bool includePolyline,
            CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                _logger.LogError(
                    "GoogleMaps:ApiKey is not configured. No travel time can be calculated.");

                return null;
            }

            var trafficAware = mode == RoutingTrafficMode.Precision;

            var body = new StringBuilder();
            body.Append("{\"origin\":{\"location\":{\"latLng\":{\"latitude\":")
                .Append(Inv(originLat))
                .Append(",\"longitude\":")
                .Append(Inv(originLng))
                .Append("}}},\"destination\":{\"location\":{\"latLng\":{\"latitude\":")
                .Append(Inv(destLat))
                .Append(",\"longitude\":")
                .Append(Inv(destLng))
                .Append("}}},\"travelMode\":\"DRIVE\",\"routingPreference\":\"")
                .Append(trafficAware ? "TRAFFIC_AWARE" : "TRAFFIC_UNAWARE")
                .Append('"');

            if (trafficAware && departureUtc.HasValue)
            {
                // Google refuses a departure in the past. A trip whose hour has already gone by —
                // a late route being re-planned — is priced as leaving in a minute, which is the
                // honest answer for a vehicle that is about to leave anyway.
                var departure = departureUtc.Value <= DateTime.UtcNow
                    ? DateTime.UtcNow.AddMinutes(1)
                    : departureUtc.Value;

                body.Append(",\"departureTime\":\"")
                    .Append(departure.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture))
                    .Append('"');
            }

            body.Append('}');

            using var request = new HttpRequestMessage(HttpMethod.Post, Endpoint)
            {
                Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json")
            };

            request.Headers.Add("X-Goog-Api-Key", _apiKey);
            request.Headers.Add(
                "X-Goog-FieldMask",
                includePolyline ? FieldMaskWithPolyline : FieldMask);

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);

                var payload = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    // ⚠️ Status and Google's own message only. The body of a routing request
                    // carries the coordinates of a patient's home, and this line goes to a log
                    // file. See the PHI rule in the constitution.
                    _logger.LogError(
                        "Routes API refused a request: {Status}. {Message}",
                        (int)response.StatusCode,
                        ExtractErrorMessage(payload));

                    return null;
                }

                return Parse(payload, trafficAware);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                // A leg nobody could price is not a failed request: the dispatcher keeps the
                // value already on screen and the route stays usable.
                _logger.LogError(ex, "Routes API call failed.");

                return null;
            }
        }

        private static GoogleLegResult? Parse(string payload, bool trafficAware)
        {
            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("routes", out var routes)
                || routes.ValueKind != JsonValueKind.Array
                || routes.GetArrayLength() == 0)
            {
                // An empty routes array is Google saying there is no road between these points —
                // two coordinates in different countries, or a pin dropped in water.
                return null;
            }

            var route = routes[0];

            var duration = ReadSeconds(route, "duration");
            var staticDuration = ReadSeconds(route, "staticDuration");

            var distance = route.TryGetProperty("distanceMeters", out var meters)
                && meters.TryGetInt32(out var parsedMeters)
                    ? parsedMeters
                    : 0;

            // On a traffic-aware request `duration` includes traffic and `staticDuration` does
            // not. On a traffic-unaware one they are the same thing, so free-flow is whichever
            // came back.
            var freeFlow = staticDuration ?? duration;

            if (freeFlow is null) return null;

            string? polyline = null;

            if (route.TryGetProperty("polyline", out var polylineElement)
                && polylineElement.TryGetProperty("encodedPolyline", out var encoded))
            {
                polyline = encoded.GetString();
            }

            return new GoogleLegResult
            {
                DurationSeconds = freeFlow.Value,
                DurationInTrafficSeconds = trafficAware ? duration ?? freeFlow : null,
                DistanceMeters = distance,
                EncodedPolyline = polyline
            };
        }

        /// <summary>Google returns durations as <c>"1234s"</c>, seconds with a suffix.</summary>
        private static int? ReadSeconds(JsonElement route, string property)
        {
            if (!route.TryGetProperty(property, out var element)) return null;

            var raw = element.GetString();

            if (string.IsNullOrWhiteSpace(raw)) return null;

            var digits = raw.TrimEnd('s', 'S');

            return double.TryParse(digits, NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
                ? (int)Math.Round(seconds)
                : null;
        }

        private static string ExtractErrorMessage(string payload)
        {
            try
            {
                using var document = JsonDocument.Parse(payload);

                if (document.RootElement.TryGetProperty("error", out var error)
                    && error.TryGetProperty("message", out var message))
                {
                    return message.GetString() ?? "(no message)";
                }
            }
            catch (JsonException)
            {
                // Not JSON — an HTML error page from a proxy, most likely.
            }

            return "(no message)";
        }

        private static string Inv(double value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
