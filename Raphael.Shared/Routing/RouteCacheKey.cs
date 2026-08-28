using System.Globalization;
using System.Text;
using Raphael.Shared.Entities.Routing;

namespace Raphael.Shared.Routing
{
    /// <summary>
    /// How a leg or an address becomes a cache key.
    /// </summary>
    /// <remarks>
    /// Every hit in the routing cache is a request nobody paid for, and every near-miss is one
    /// somebody did. The rounding and bucketing rules live here, alone, because the writer and the
    /// reader disagreeing by one decimal place would silently turn the cache off.
    /// </remarks>
    public static class RouteCacheKey
    {
        private const double MetersPerMile = 1609.344;

        /// <summary>
        /// A coordinate as the integer the cache keys on: four decimals, about 11 metres.
        /// </summary>
        /// <remarks>
        /// Geocoding the same address twice can return points a few metres apart, and so can two
        /// GPS fixes in the same parking lot. Four decimals treats those as the same place. Five
        /// would treat them as different ones and buy the same road again.
        /// </remarks>
        public static int ToE4(double coordinate) =>
            (int)Math.Round(coordinate * 10_000, MidpointRounding.AwayFromZero);

        /// <summary>Metres to miles, in one place, so every screen shows the same number.</summary>
        public static double ToMiles(int meters) =>
            Math.Round(meters / MetersPerMile, 2, MidpointRounding.AwayFromZero);

        /// <summary>
        /// Which time bucket and day type an answer should be filed under.
        /// </summary>
        /// <remarks>
        /// In <see cref="RoutingTrafficMode.MaxSavings"/> the answer carries no traffic, so it does
        /// not depend on when the vehicle leaves: it is filed once per pair of points and served
        /// all week. That single decision is most of why this mode is cheap — bucketing a
        /// time-independent answer by hour would mean buying the same road up to 48 times.
        /// </remarks>
        /// <param name="mode">The mode the answer was, or will be, bought under.</param>
        /// <param name="localDeparture">
        /// Departure in business wall-clock time. Null means now, and only matters in Precision.
        /// </param>
        public static (byte TimeBucket, byte DayType) BucketFor(
            RoutingTrafficMode mode,
            DateTime? localDeparture)
        {
            if (mode == RoutingTrafficMode.MaxSavings)
            {
                return (RouteLegCacheEntry.NotTimeDependent, RouteLegCacheEntry.NotTimeDependent);
            }

            var departure = localDeparture ?? DateTime.MinValue;

            if (localDeparture is null)
            {
                // Precision with no stated departure is "leaving now", and the caller is the only
                // one who knows what now is where the vehicle is. Treat it as uncacheable by hour
                // rather than guess a bucket from the server's clock.
                return (RouteLegCacheEntry.NotTimeDependent, RouteLegCacheEntry.NotTimeDependent);
            }

            var dayType = departure.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? (byte)1
                : (byte)0;

            return ((byte)departure.Hour, dayType);
        }

        /// <summary>
        /// The bucket an observed time is filed under. Always by the hour it happened — these are
        /// our own measurements and the hour is the whole point of keeping them.
        /// </summary>
        public static (byte TimeBucket, byte DayType) ObservedBucketFor(DateTime localDeparture)
        {
            var dayType = localDeparture.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
                ? (byte)1
                : (byte)0;

            return ((byte)localDeparture.Hour, dayType);
        }

        /// <summary>
        /// An address reduced to what makes it the same address.
        /// </summary>
        /// <remarks>
        /// "123 Main St., Naples, FL 34102" and "123 MAIN ST  Naples FL 34102" are one address and
        /// must be one row. Case, punctuation and repeated whitespace are dropped; nothing else is
        /// — abbreviations are left alone, because deciding that "St" is "Street" is a guess, and a
        /// wrong guess here sends a vehicle to another street.
        /// </remarks>
        public static string NormalizeAddress(string? address)
        {
            if (string.IsNullOrWhiteSpace(address)) return string.Empty;

            var builder = new StringBuilder(address.Length);
            var lastWasSpace = false;

            foreach (var raw in address.Trim().ToUpperInvariant())
            {
                var c = raw;

                if (c is ',' or '.' or ';' or '#') c = ' ';

                if (char.IsWhiteSpace(c))
                {
                    if (!lastWasSpace && builder.Length > 0) builder.Append(' ');
                    lastWasSpace = true;
                    continue;
                }

                builder.Append(c);
                lastWasSpace = false;
            }

            var normalized = builder.ToString().TrimEnd();

            // The column holds 300. Truncating keeps the row insertable; an address this long is
            // already a data-entry accident, and it still keys consistently.
            return normalized.Length <= 300 ? normalized : normalized[..300];
        }

        /// <summary>Builds the address line from its parts, the way the clients send them.</summary>
        public static string ComposeAddress(string? street, string? city, string? state, string? zip)
        {
            var parts = new[] { street, city, state, zip }
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim());

            return string.Join(", ", parts);
        }

        /// <summary>
        /// Formats a coordinate for a Google request: invariant, so a comma never arrives where a
        /// decimal point belongs.
        /// </summary>
        public static string Coord(double value) =>
            value.ToString("0.######", CultureInfo.InvariantCulture);
    }
}
