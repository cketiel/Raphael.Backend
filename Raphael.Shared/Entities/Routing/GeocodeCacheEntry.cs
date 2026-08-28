namespace Raphael.Shared.Entities.Routing
{
    /// <summary>
    /// One address resolved to coordinates, kept so the same address is not bought twice.
    /// </summary>
    /// <remarks>
    /// The daily CSV import is the heaviest geocoding consumer in the ecosystem, and most of what
    /// it carries is not new: the same dozen clinics appear on nearly every row, and yesterday's
    /// patients are today's patients.
    ///
    /// <para>
    /// Expires with <c>Routing.CacheRetentionDays</c> (default one year), the same dial as the
    /// route cache — an administrator's decision, consistent with the coordinates the Customers
    /// table has stored since production began. Only <see cref="PlaceId"/> is unrestricted by
    /// Google's terms either way.
    /// </para>
    /// </remarks>
    public class GeocodeCacheEntry
    {
        public int Id { get; set; }

        /// <summary>
        /// The address as the cache key: trimmed, upper-cased, punctuation and repeated spaces
        /// collapsed. Built by <c>RouteCacheKey.NormalizeAddress</c> — never assign it raw.
        /// </summary>
        public string NormalizedAddress { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        /// <summary>Google's stable identifier for the place, when it gave one.</summary>
        public string? PlaceId { get; set; }

        /// <summary>The address as Google prints it. Useful when a dispatcher disputes a pin.</summary>
        public string? FormattedAddress { get; set; }

        public GeocodeStatus Status { get; set; }

        public DateTime FetchedAtUtc { get; set; }
    }
}
