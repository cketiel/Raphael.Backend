namespace Raphael.Shared.Entities.Routing
{
    /// <summary>
    /// One travel time and distance as Google gave it, kept so the same leg is not bought twice.
    /// </summary>
    /// <remarks>
    /// NEMT is repetitive by nature: a dialysis patient travels from the same address to the same
    /// clinic three mornings a week, and every dispatcher who touches that route asks for the same
    /// leg. Without this table each of those asks is a billed request.
    ///
    /// <para>
    /// This is a cache with an expiry, not an archive. <c>RouteCachePurgeWorker</c> deletes rows
    /// older than <c>Routing.CacheRetentionDays</c> (default one year — an administrator's
    /// decision; Google's terms describe a 30-day window, and turning the dial down needs no
    /// deployment). What is ours to keep for good is what we measured ourselves: see
    /// <see cref="ObservedLegTime"/>.
    /// </para>
    /// </remarks>
    public class RouteLegCacheEntry
    {
        public long Id { get; set; }

        /// <summary>Origin latitude × 10,000, rounded.</summary>
        /// <remarks>
        /// Four decimals is about 11 metres. It absorbs the jitter between the coordinates
        /// geocoding returns for one address on two different days, without merging a house with
        /// its neighbour. Five decimals — 1.1 m — would fragment the cache and collapse the hit
        /// rate on exactly the recurring patients this table exists for.
        /// </remarks>
        public int OriginLatE4 { get; set; }

        public int OriginLngE4 { get; set; }

        public int DestLatE4 { get; set; }

        public int DestLngE4 { get; set; }

        /// <summary>Hour of the business day, 0–23, or <see cref="NotTimeDependent"/>.</summary>
        /// <remarks>
        /// A duration with no traffic in it does not change with the hour, so a
        /// <see cref="RoutingTrafficMode.MaxSavings"/> answer is stored once per pair of points
        /// and serves all day. Bucketing it by hour anyway would mean paying for the same road
        /// twenty-four times.
        /// </remarks>
        public byte TimeBucket { get; set; }

        /// <summary>0 = weekday, 1 = weekend, or <see cref="NotTimeDependent"/>.</summary>
        /// <remarks>
        /// The operation runs Monday to Friday. Splitting the week into seven would multiply the
        /// cells to fill by three and a half without Tuesday's traffic differing from Thursday's.
        /// The column is a byte: going to seven values later changes no schema.
        /// </remarks>
        public byte DayType { get; set; }

        /// <summary>The mode this answer was bought under. Answers of the two modes never mix.</summary>
        public RoutingTrafficMode TrafficMode { get; set; }

        /// <summary>Free-flow driving time, seconds.</summary>
        public int DurationSeconds { get; set; }

        /// <summary>
        /// Driving time with traffic, seconds. Only filled in
        /// <see cref="RoutingTrafficMode.Precision"/>; null means nobody paid for traffic here.
        /// </summary>
        public int? DurationInTrafficSeconds { get; set; }

        public int DistanceMeters { get; set; }

        /// <summary>
        /// The route's shape, as Google's encoded polyline, or null when nobody asked for one.
        /// </summary>
        /// <remarks>
        /// Only the map screens want a shape, and only they pay the extra field. Everything that
        /// merely schedules — the ETA chain, the driver's next legs — asks for duration and
        /// distance and leaves this null. A row bought without a shape is still a hit for those
        /// callers; it is a miss only for a caller that needs to draw the road.
        /// </remarks>
        public string? EncodedPolyline { get; set; }

        /// <summary>When Google answered. The clock the retention purge runs on.</summary>
        public DateTime FetchedAtUtc { get; set; }

        /// <summary>
        /// Stored in <see cref="TimeBucket"/> and <see cref="DayType"/> when the answer does not
        /// depend on when the vehicle leaves.
        /// </summary>
        public const byte NotTimeDependent = 255;
    }
}
