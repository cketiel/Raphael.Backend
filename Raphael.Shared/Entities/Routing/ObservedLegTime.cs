namespace Raphael.Shared.Entities.Routing
{
    /// <summary>
    /// How long a leg actually took, measured by our own vehicles.
    /// </summary>
    /// <remarks>
    /// This is the one table here that is ours to keep. Google's terms restrict what may be
    /// retained from their answers; nothing restricts what our own drivers measured on our own
    /// routes. So this table has no expiry, and it is the foundation the automatic router will be
    /// built on — a router needs to know what a leg costs at eight in the morning, and after a
    /// season of operation these rows know that better than any prediction.
    ///
    /// <para>
    /// It also pays for itself before the router exists: the buffer that
    /// <see cref="RoutingTrafficMode.MaxSavings"/> adds to a free-flow duration is calibrated from
    /// these rows, per hour of the day. The more the fleet drives, the less the estimate depends
    /// on anything bought.
    /// </para>
    ///
    /// <para>
    /// Rows are kept raw rather than pre-aggregated. At roughly 160,000 a year that is nothing for
    /// SQL Server, and it leaves exact percentiles available — a median and an 85th are what a
    /// router wants, and a running sum cannot produce either after the fact.
    /// </para>
    /// </remarks>
    public class ObservedLegTime
    {
        public long Id { get; set; }

        /// <summary>Origin latitude × 10,000, rounded — the same key shape as the route cache.</summary>
        public int OriginLatE4 { get; set; }

        public int OriginLngE4 { get; set; }

        public int DestLatE4 { get; set; }

        public int DestLngE4 { get; set; }

        /// <summary>Hour of the business day the vehicle departed, 0–23.</summary>
        public byte TimeBucket { get; set; }

        /// <summary>0 = weekday, 1 = weekend.</summary>
        public byte DayType { get; set; }

        /// <summary>
        /// Measured driving time, seconds: from leaving the previous stop to arriving at this one.
        /// Service time at the previous stop is excluded.
        /// </summary>
        public int DurationSeconds { get; set; }

        /// <summary>Planned distance for the leg, when it was known. Not measured by odometer.</summary>
        public int? DistanceMeters { get; set; }

        public ObservedLegSource Source { get; set; }

        /// <summary>Which route drove it. Kept so an outlier can be traced back to a shift.</summary>
        public int? VehicleRouteId { get; set; }

        /// <summary>The stop that was arrived at.</summary>
        public int? ScheduleId { get; set; }

        public DateTime ObservedAtUtc { get; set; }
    }
}
