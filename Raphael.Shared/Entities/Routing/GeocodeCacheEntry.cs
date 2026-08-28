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
    /// ⚠️ Temporary cache. Google's terms allow latitude and longitude to be kept for at most 30
    /// consecutive days and require deletion afterwards; only <see cref="PlaceId"/> may be kept
    /// indefinitely. The whole row is deleted at 30 days — keeping a place id with no coordinates
    /// would save nothing, since resolving it back costs a request either way.
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
