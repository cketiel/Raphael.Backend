using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;

namespace Raphael.Api.Services.Auth
{
    /// <summary>
    /// The client applications, as they name themselves in <c>X-Client-App</c>, and which of them
    /// each kind of user may use (RE-048, PROVIDER_ACCESS_MODEL.md §3.1).
    /// </summary>
    public static class ClientApps
    {
        public const string Desktop = "Desktop";
        public const string Driver = "Driver";
        public const string Booking = PortalClaims.BookingApp;
        public const string Rider = "Rider";

        /// <summary>No header, or one this API does not know.</summary>
        public const string Unknown = "Unknown";

        /// <summary>The token claim that records which application the session was opened for.</summary>
        public const string Claim = "app";

        /// <summary>The role id of a driver.</summary>
        private const int DriverRoleId = 2;

        private static readonly string[] Known = { Desktop, Driver, Booking, Rider };

        /// <summary>The header as one of the known names, or <see cref="Unknown"/>.</summary>
        public static string Normalize(string? header) =>
            Known.FirstOrDefault(k => string.Equals(k, header?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? Unknown;

        /// <summary>
        /// The applications a staff user may sign in to. Decided by who the user is, never by the
        /// client: a clinic's user and an external Provider's only enter Booking; the office never
        /// does (P1); a driver uses the Driver and also the Desktop, for its reports (decided
        /// 2026-10-13).
        /// </summary>
        public static IReadOnlyCollection<string> AllowedFor(User user, ProviderWorkMode? providerMode)
        {
            if (user.IntegratorId != null || providerMode == ProviderWorkMode.External)
            {
                return new[] { Booking };
            }

            return user.RoleId == DriverRoleId
                ? new[] { Driver, Desktop }
                : new[] { Desktop };
        }
    }
}
