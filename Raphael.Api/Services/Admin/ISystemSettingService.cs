using Raphael.Shared.DTOs.Routing;

namespace Raphael.Api.Services.Admin
{
    /// <summary>
    /// Reads and writes the settings an administrator can change without a deployment.
    /// </summary>
    public interface ISystemSettingService
    {
        /// <summary>
        /// The current value, or <paramref name="fallback"/> when the key has never been set.
        /// </summary>
        /// <remarks>
        /// Cached briefly. A screen pricing thirty legs must not query thirty times, and a change
        /// still lands within the minute.
        /// </remarks>
        Task<string> GetAsync(string key, string fallback, CancellationToken cancellationToken = default);

        Task<int> GetIntAsync(string key, int fallback, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(CancellationToken cancellationToken = default);

        Task<SystemSettingDto?> GetOneAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>Writes a value and drops the cached copy, so the change is live at once.</summary>
        Task<SystemSettingDto> SetAsync(
            string key,
            string value,
            string? updatedBy,
            CancellationToken cancellationToken = default);
    }

    /// <summary>The keys this system understands, and what they may hold.</summary>
    public static class SystemSettingKeys
    {
        /// <summary>
        /// <c>MaxSavings</c> or <c>Precision</c>. Decides whether travel times are bought with
        /// traffic. See <c>RoutingTrafficMode</c>.
        /// </summary>
        public const string RoutingTrafficMode = "Routing.TrafficMode";

        /// <summary>
        /// Whole percent added to a free-flow duration in MaxSavings mode. Until there are enough
        /// observed times to calibrate per hour, this flat figure is what stands between a
        /// free-flow estimate and a driver who is always late.
        /// </summary>
        public const string RoutingDefaultBufferPercent = "Routing.DefaultBufferPercent";

        /// <summary>
        /// How many days a cached Google answer is kept and served before the purge deletes it.
        /// Default 365. An administrator's decision: Google's terms describe a 30-day window for
        /// cached content, and the business chose a year — the same posture the Customers table
        /// has taken since production began. Shortening it later needs no deployment.
        /// </summary>
        public const string RoutingCacheRetentionDays = "Routing.CacheRetentionDays";
    }
}
