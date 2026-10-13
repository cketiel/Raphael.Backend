namespace Raphael.Shared.Helpers
{
    /// <summary>
    /// The claims that keep a portal user (a clinic's, or an external Provider's) out of the office.
    /// </summary>
    /// <remarks>
    /// Written on every token from the database, at sign-in and at every refresh, so a change of
    /// work mode or a password change reaches the next token without a new sign-in.
    /// PROVIDER_ACCESS_MODEL.md §3.
    /// </remarks>
    public static class PortalClaims
    {
        /// <summary>The Provider's work mode, only on users of a Provider: "External" or "Raphael".</summary>
        public const string ProviderMode = "UserProviderMode";

        public const string External = "External";

        /// <summary>"true" while the account may only change its password.</summary>
        public const string MustChangePassword = "MustChangePassword";

        /// <summary>
        /// The tenant (client company) a user is held to: its <c>ProviderId</c> when that Provider
        /// is a tenant, empty otherwise. Written from the database on every issue (RE-049).
        /// </summary>
        public const string TenantId = "TenantId";

        /// <summary>The Booking portal's X-Client-App: the only app an external Provider's user enters.</summary>
        public const string BookingApp = "BookingWeb";
    }
}
