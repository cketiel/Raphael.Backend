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
        /// The cache key. Three shapes share this table, told apart by their prefix:
        /// </summary>
        /// <remarks>
        /// <list type="bullet">
        /// <item>a plain address — trimmed, upper-cased, punctuation collapsed, built by
        /// <c>RouteCacheKey.NormalizeAddress</c>;</item>
        /// <item><c>@lat,lng</c> — a point somebody dropped a pin on, rounded, for reverse
        /// geocoding;</item>
        /// <item><c>place:&lt;id&gt;</c> — a Google place chosen from the autocomplete.</item>
        /// </list>
        /// One table rather than three because all three answer the same question — where is
        /// this — and all three expire under the same retention setting and the same purge.
        /// Never assign this raw: use the builders on <c>RouteCacheKey</c>.
        /// </remarks>
        public string NormalizedAddress { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        /// <summary>Google's stable identifier for the place, when it gave one.</summary>
        public string? PlaceId { get; set; }

        /// <summary>The address as Google prints it. Useful when a dispatcher disputes a pin.</summary>
        public string? FormattedAddress { get; set; }

        /// <summary>
        /// The address broken into the four fields every form in this application stores.
        /// </summary>
        /// <remarks>
        /// Kept so a cached answer can fill a customer form without asking Google again. Before
        /// these existed the map had the coordinates but not the street, and went back to Google
        /// for a line it had already been told.
        /// </remarks>
        public string? Street { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Zip { get; set; }

        public GeocodeStatus Status { get; set; }

        public DateTime FetchedAtUtc { get; set; }
    }
}
