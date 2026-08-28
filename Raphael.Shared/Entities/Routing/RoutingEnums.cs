namespace Raphael.Shared.Entities.Routing
{
    /// <summary>
    /// How much the business is willing to pay for a travel time.
    /// </summary>
    /// <remarks>
    /// Google bills a route request that asks for traffic at twice the rate of one that does
    /// not, and with a free monthly allowance half the size. That is a business decision, not
    /// a constant: it changes with the season, with the contract, and with how much the office
    /// trusts its own historical numbers. It lives in <c>SystemSettings</c> under
    /// <c>Routing.TrafficMode</c> and an administrator changes it without a deployment.
    /// </remarks>
    public enum RoutingTrafficMode : byte
    {
        /// <summary>
        /// Ask Google for the free-flow time only, then add a buffer of our own.
        /// </summary>
        /// <remarks>
        /// The cheaper request, and the one that caches best: a duration with no traffic in it
        /// does not depend on the hour, so one answer serves every departure time for that pair
        /// of points. The hour comes back in through the buffer, which is ours to calibrate
        /// from <see cref="ObservedLegTime"/>.
        /// </remarks>
        MaxSavings = 0,

        /// <summary>
        /// Ask Google for the traffic-aware time at the hour the vehicle actually departs.
        /// </summary>
        /// <remarks>
        /// Twice the price per request and cached per time bucket, so it needs far more
        /// requests to fill. Worth it when the office is planning around congestion it cannot
        /// yet predict from its own data.
        /// </remarks>
        Precision = 1
    }

    /// <summary>
    /// What Google said about an address, kept so that a bad address is not paid for twice.
    /// </summary>
    public enum GeocodeStatus : byte
    {
        /// <summary>Coordinates were returned.</summary>
        Ok = 0,

        /// <summary>
        /// Google understood the request and found nothing. Cached deliberately: a CSV with a
        /// mistyped address is re-imported every morning, and without this the same failure is
        /// billed twenty-one times a month.
        /// </summary>
        ZeroResults = 1
    }

    /// <summary>Where an observed travel time was measured.</summary>
    public enum ObservedLegSource : byte
    {
        /// <summary>
        /// Derived from two consecutive performed stops: the driver finished the previous stop
        /// and arrived at this one. This is the driving time, service time excluded.
        /// </summary>
        SchedulePerformed = 0,

        /// <summary>Derived from the vehicle's own GPS trail. Not produced yet.</summary>
        Gps = 1
    }
}
