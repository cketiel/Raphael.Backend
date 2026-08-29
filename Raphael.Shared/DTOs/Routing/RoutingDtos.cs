namespace Raphael.Shared.DTOs.Routing
{
    /// <summary>
    /// The whole contract of the routing proxy, in one file on purpose.
    /// </summary>
    /// <remarks>
    /// Desktop and Driver each keep a hand-written copy of these types, and a contract split
    /// across eight files is a contract that gets copied seven-eighths of the way. Keeping it
    /// together means one file to mirror and one diff to read when it changes.
    /// </remarks>
    public static class RoutingContract
    {
        /// <summary>
        /// Values of <see cref="RouteLegResultDto.Source"/>: where the number came from, and
        /// nothing else.
        /// </summary>
        /// <remarks>
        /// ⚠️ There used to be a third value, <c>Buffered</c>, and it made this field unusable.
        /// It answered a different question — how the planning figure was derived — so in
        /// MaxSavings mode *every* answer read "Buffered" whether it had been bought from Google
        /// or served free from the cache, and nothing downstream could tell the difference. That
        /// is now <see cref="RouteLegResultDto.Buffered"/>, a separate flag, and this field means
        /// only what its name says: was anybody billed for this.
        /// </remarks>
        public static class Sources
        {
            /// <summary>Served from our cache. Nobody was billed for it.</summary>
            public const string Cache = "Cache";

            /// <summary>Bought from Google on this request.</summary>
            public const string Google = "Google";
        }

        /// <summary>Values of the per-item <c>Status</c> fields.</summary>
        public static class Statuses
        {
            public const string Ok = "Ok";

            /// <summary>
            /// No answer for this item. The others in the same batch are still valid — a caller
            /// must keep whatever value it already had rather than write a zero.
            /// </summary>
            public const string Unavailable = "Unavailable";

            /// <summary>The address is well-formed and Google knows no such place.</summary>
            public const string NotFound = "NotFound";
        }
    }

    /// <summary>One leg to price: a drive from one point to another, leaving at a given time.</summary>
    public class RouteLegRequestItemDto
    {
        public double OriginLat { get; set; }

        public double OriginLng { get; set; }

        public double DestLat { get; set; }

        public double DestLng { get; set; }

        /// <summary>
        /// The service date of the leg. Together with <see cref="DepartureTime"/> this is business
        /// wall-clock time — 07:30 means 07:30 where the vehicle is, whoever asked.
        /// </summary>
        /// <remarks>
        /// ⚠️ Both may be omitted, and then the leg is priced as leaving now. Omit them only for
        /// something happening now: a trip planned the evening before, priced against this
        /// evening's traffic, is a wrong answer that looks right.
        /// </remarks>
        public DateTime? Date { get; set; }

        public TimeSpan? DepartureTime { get; set; }

        /// <summary>
        /// Ask for the road's shape as well, so a map can draw it. Off by default.
        /// </summary>
        /// <remarks>
        /// Only a screen showing a map has any use for this. Scheduling asks for time and distance
        /// and nothing else, which keeps the field mask — and the cached row — small. A cached
        /// answer without a shape does not satisfy a request that set this.
        /// </remarks>
        public bool IncludePolyline { get; set; }
    }

    public class RouteLegsRequestDto
    {
        public List<RouteLegRequestItemDto> Legs { get; set; } = new();
    }

    /// <summary>What a leg costs in time and distance.</summary>
    public class RouteLegResultDto
    {
        /// <summary>Free-flow driving time, seconds.</summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Driving time to plan against, seconds: Google's traffic estimate in Precision mode,
        /// our own buffered figure in MaxSavings. Null only when <see cref="Status"/> is not Ok.
        /// </summary>
        public int? DurationInTrafficSeconds { get; set; }

        public int DistanceMeters { get; set; }

        /// <summary>
        /// The same distance in miles, converted here so that every client shows the same number.
        /// </summary>
        /// <remarks>
        /// The clients used to parse Google's own <c>"12.3 mi"</c> string with the machine's
        /// locale, which on a Spanish-locale machine read 12.3 as 123.
        /// </remarks>
        public double DistanceMiles { get; set; }

        /// <summary>
        /// The road's shape as an encoded polyline, when it was asked for. Null otherwise.
        /// </summary>
        /// <remarks>
        /// This replaces what the Desktop's map used to get from the JavaScript
        /// <c>DirectionsService</c> — a class Google no longer serves to projects created after
        /// March 2025. Drawing it costs the map a <c>google.maps.Polyline</c> and no billed call
        /// of its own, since the leg is already paid for or cached here.
        /// </remarks>
        public string? EncodedPolyline { get; set; }

        /// <summary>One of <see cref="RoutingContract.Sources"/>. Was anybody billed for this.</summary>
        public string Source { get; set; } = RoutingContract.Sources.Cache;

        /// <summary>
        /// True when <see cref="DurationInTrafficSeconds"/> is our own free-flow-plus-margin
        /// figure rather than a traffic estimate from Google. No screen should present a buffered
        /// number as Google's.
        /// </summary>
        /// <remarks>
        /// Independent of <see cref="Source"/>: a buffered answer can be freshly bought or come
        /// from the cache, and a Google traffic figure likewise.
        /// </remarks>
        public bool Buffered { get; set; }

        /// <summary>One of <see cref="RoutingContract.Statuses"/>.</summary>
        public string Status { get; set; } = RoutingContract.Statuses.Ok;
    }

    /// <summary>
    /// Answers in the same order as the legs asked for, always the same count.
    /// </summary>
    public class RouteLegsResponseDto
    {
        /// <summary>
        /// The mode these answers were produced under, so a client can tell a real traffic
        /// estimate from a buffered one without inspecting every leg.
        /// </summary>
        public string TrafficMode { get; set; } = string.Empty;

        public List<RouteLegResultDto> Legs { get; set; } = new();
    }

    /// <summary>An address to resolve. Either the whole line, or its parts.</summary>
    public class GeocodeRequestDto
    {
        public string? Address { get; set; }

        public string? Street { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Zip { get; set; }
    }

    public class GeocodeBatchRequestDto
    {
        public List<string> Addresses { get; set; } = new();
    }

    public class GeocodeResultDto
    {
        /// <summary>The address as it was asked for, so a batch answer can be matched up.</summary>
        public string Address { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public string? PlaceId { get; set; }

        public string? FormattedAddress { get; set; }

        /// <summary>One of <see cref="RoutingContract.Statuses"/>.</summary>
        public string Status { get; set; } = RoutingContract.Statuses.Ok;

        /// <summary>One of <see cref="RoutingContract.Sources"/>.</summary>
        public string Source { get; set; } = RoutingContract.Sources.Cache;
    }

    public class GeocodeBatchResponseDto
    {
        public List<GeocodeResultDto> Results { get; set; } = new();
    }

    public class ReverseGeocodeRequestDto
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }
    }

    public class ReverseGeocodeResultDto
    {
        /// <summary>The locality, or null when the point is not in one.</summary>
        public string? City { get; set; }

        public string Status { get; set; } = RoutingContract.Statuses.Ok;

        public string Source { get; set; } = RoutingContract.Sources.Cache;
    }

    /// <summary>A setting as the admin panel shows it.</summary>
    public class SystemSettingDto
    {
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public string? UpdatedBy { get; set; }
    }

    public class SystemSettingUpdateDto
    {
        public string Value { get; set; } = string.Empty;
    }
}
