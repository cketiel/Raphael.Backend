namespace Raphael.Shared.Entities
{
    /// <summary>
    /// A setting an administrator can change while the system is running.
    /// </summary>
    /// <remarks>
    /// For decisions that belong to the business rather than to the build. The first of them is
    /// <c>Routing.TrafficMode</c>: how much the office is willing to pay Google for a travel time.
    /// That answer changes with the season and with the contract, and waiting for a deployment to
    /// change it would mean it never changes.
    ///
    /// <para>
    /// Read through <c>ISystemSettingService</c>, which caches for a minute — so a change lands
    /// within a minute everywhere, and a busy screen does not query for every leg it prices.
    /// </para>
    ///
    /// <para>
    /// ⚠️ Not a place for secrets. Anything an administrator can read in a panel is not a secret;
    /// keys and connection strings stay in User Secrets and environment variables.
    /// </para>
    /// </remarks>
    public class SystemSetting
    {
        public int Id { get; set; }

        /// <summary>Dotted name, e.g. <c>Routing.TrafficMode</c>. Unique.</summary>
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        /// <summary>What this setting is for, shown next to it in the admin panel.</summary>
        public string? Description { get; set; }

        public DateTime UpdatedAtUtc { get; set; }

        /// <summary>Who changed it last. A setting that moves money should name someone.</summary>
        public string? UpdatedBy { get; set; }
    }
}
