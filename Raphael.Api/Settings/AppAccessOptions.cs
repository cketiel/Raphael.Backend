namespace Raphael.Api.Settings
{
    /// <summary>How strictly each user is held to the application that belongs to it (RE-048).</summary>
    public enum AppAccessMode
    {
        /// <summary>Nothing is checked. What production runs until Audit has come back clean.</summary>
        Off,

        /// <summary>Everything is checked and every miss is logged, but nothing is refused.</summary>
        Audit,

        /// <summary>Misses are refused with 403 <c>wrong_app</c>.</summary>
        Enforce
    }

    /// <summary>
    /// Which application a user may sign in to, and which applications each endpoint admits.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before this the API admitted any authenticated token anywhere: a clinic's user could open
    /// the Desktop, and with the token the portal hands its browser could reach the office's routes,
    /// vehicles and drivers (PROVIDER_ACCESS_MODEL.md §2, F-A to F-D). The rule is inverted: an
    /// endpoint admits the Desktop unless it says otherwise with <c>[AllowApps]</c>.
    /// </para>
    /// <para>
    /// ⚠️ Going from Audit to Enforce: a token issued before this release carries no <c>app</c>
    /// claim and reads as <c>Unknown</c>, which Enforce refuses. Switch only once the longest
    /// access-token lifetime in <c>SessionPolicy</c> has passed since the deployment; by then every
    /// session has renewed and carries the claim. Audit shows it: no more <c>Unknown</c> lines.
    /// </para>
    /// <para>In Azure: <c>AppAccess__Mode</c> = <c>Off</c> | <c>Audit</c> | <c>Enforce</c>.</para>
    /// </remarks>
    public sealed class AppAccessOptions
    {
        public const string SectionName = "AppAccess";

        public AppAccessMode Mode { get; set; } = AppAccessMode.Off;
    }
}
