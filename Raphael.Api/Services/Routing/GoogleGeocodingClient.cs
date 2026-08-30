using System.Text.Json;

namespace Raphael.Api.Services.Routing
{
    /// <summary>An address as Google resolved it.</summary>
    public sealed class GoogleGeocodeResult
    {
        public double Latitude { get; init; }

        public double Longitude { get; init; }

        public string? PlaceId { get; init; }

        public string? FormattedAddress { get; init; }

        /// <summary>Street line, from the street number and route components.</summary>
        public string? Street { get; init; }

        public string? City { get; init; }

        public string? State { get; init; }

        public string? Zip { get; init; }
    }

    /// <summary>
    /// The only place in the ecosystem that turns an address into coordinates.
    /// </summary>
    /// <remarks>
    /// ⚠️ The address travels to Google in a query string — the Geocoding API takes no key or
    /// payload in headers, so there is no way around it. That is exactly why nothing here ever
    /// logs the address, the URL, or the response body: what leaves for Google under TLS is one
    /// thing, what lands in a log file that someone later greps is another. See the PHI rule in
    /// the constitution.
    /// </remarks>
    public class GoogleGeocodingClient
    {
        private const string GeocodeEndpoint = "https://maps.googleapis.com/maps/api/geocode/json";

        private readonly HttpClient _http;
        private readonly ILogger<GoogleGeocodingClient> _logger;
        private readonly string? _apiKey;

        public GoogleGeocodingClient(
            HttpClient http,
            IConfiguration configuration,
            ILogger<GoogleGeocodingClient> logger)
        {
            _http = http;
            _logger = logger;
            _apiKey = configuration["GoogleMaps:ApiKey"];

            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

        /// <summary>
        /// Resolves an address. Returns <c>(null, true)</c> when Google is certain there is no
        /// such place — a distinction worth keeping, because "no such address" is worth caching
        /// and "we could not ask" is not.
        /// </summary>
        public async Task<(GoogleGeocodeResult? Result, bool DefinitiveNotFound)> GeocodeAsync(
            string address,
            CancellationToken cancellationToken)
        {
            if (!IsConfigured)
            {
                _logger.LogError("GoogleMaps:ApiKey is not configured. No address can be resolved.");

                return (null, false);
            }

            var url = $"{GeocodeEndpoint}?address={Uri.EscapeDataString(address)}&key={_apiKey}";

            var payload = await GetAsync(url, cancellationToken);

            if (payload is null) return (null, false);

            using var document = JsonDocument.Parse(payload);

            var status = document.RootElement.TryGetProperty("status", out var statusElement)
                ? statusElement.GetString()
                : null;

            if (status == "ZERO_RESULTS") return (null, true);

            if (status != "OK")
            {
                // OVER_QUERY_LIMIT and REQUEST_DENIED are configuration or billing problems, not
                // bad addresses, and caching them would hide the real fault for thirty days.
                _logger.LogError("Geocoding API returned status {Status}.", status);

                return (null, false);
            }

            if (!document.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array
                || results.GetArrayLength() == 0)
            {
                return (null, true);
            }

            var first = results[0];

            if (!first.TryGetProperty("geometry", out var geometry)
                || !geometry.TryGetProperty("location", out var location)
                || !location.TryGetProperty("lat", out var lat)
                || !location.TryGetProperty("lng", out var lng))
            {
                return (null, false);
            }

            return Read(first);
        }

        /// <summary>
        /// The address a point sits at: street, town, state, postcode and the formatted line.
        /// </summary>
        /// <remarks>
        /// This used to return only the town, which meant the map's pin-drag had to ask Google
        /// itself for the rest — a billed request from the browser that no cache ever saw. It
        /// returns the whole address now so the map can be served entirely from here.
        /// </remarks>
        public async Task<GoogleGeocodeResult?> ReverseGeocodeAsync(
            double latitude,
            double longitude,
            CancellationToken cancellationToken)
        {
            if (!IsConfigured) return null;

            var latLng = $"{Raphael.Shared.Routing.RouteCacheKey.Coord(latitude)}," +
                         $"{Raphael.Shared.Routing.RouteCacheKey.Coord(longitude)}";

            var url = $"{GeocodeEndpoint}?latlng={Uri.EscapeDataString(latLng)}&key={_apiKey}";

            var payload = await GetAsync(url, cancellationToken);

            if (payload is null) return null;

            using var document = JsonDocument.Parse(payload);

            if (!document.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array
                || results.GetArrayLength() == 0)
            {
                return null;
            }

            // Google orders reverse results from the most precise to the vaguest, so the first
            // one is the street address and the last is the country.
            var read = Read(results[0]).Result;

            if (read is null) return null;

            // The point asked about, not the one Google rounded to the middle of the building.
            // Moving the pin under the dispatcher's cursor would be its own small betrayal.
            return new GoogleGeocodeResult
            {
                Latitude = latitude,
                Longitude = longitude,
                PlaceId = read.PlaceId,
                FormattedAddress = read.FormattedAddress,
                Street = read.Street,
                City = read.City,
                State = read.State,
                Zip = read.Zip
            };
        }

        /// <summary>Pulls one Google result apart into the fields this application stores.</summary>
        private static (GoogleGeocodeResult? Result, bool DefinitiveNotFound) Read(JsonElement first)
        {
            if (!first.TryGetProperty("geometry", out var geometry)
                || !geometry.TryGetProperty("location", out var location)
                || !location.TryGetProperty("lat", out var lat)
                || !location.TryGetProperty("lng", out var lng))
            {
                return (null, false);
            }

            string street = string.Empty, city = null, state = null, zip = null;

            if (first.TryGetProperty("address_components", out var components)
                && components.ValueKind == JsonValueKind.Array)
            {
                foreach (var component in components.EnumerateArray())
                {
                    if (!component.TryGetProperty("types", out var types)) continue;

                    var names = types.EnumerateArray().Select(t => t.GetString()).ToList();

                    string Long() => component.TryGetProperty("long_name", out var v) ? v.GetString() : null;
                    string Short() => component.TryGetProperty("short_name", out var v) ? v.GetString() : null;

                    if (names.Contains("street_number")) street = Long() + " ";
                    if (names.Contains("route")) street += Long();
                    if (names.Contains("locality")) city = Long();
                    if (names.Contains("administrative_area_level_1")) state = Short();
                    if (names.Contains("postal_code")) zip = Long();
                }
            }

            return (new GoogleGeocodeResult
            {
                Latitude = lat.GetDouble(),
                Longitude = lng.GetDouble(),
                PlaceId = first.TryGetProperty("place_id", out var placeId) ? placeId.GetString() : null,
                FormattedAddress = first.TryGetProperty("formatted_address", out var formatted)
                    ? formatted.GetString()
                    : null,
                Street = street.Trim(),
                City = city,
                State = state,
                Zip = zip
            }, false);
        }

        private async Task<string?> GetAsync(string url, CancellationToken cancellationToken)
        {
            try
            {
                using var response = await _http.GetAsync(url, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError(
                        "Geocoding API refused a request: {Status}.",
                        (int)response.StatusCode);

                    return null;
                }

                return await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Geocoding API call failed.");

                return null;
            }
        }
    }
}
