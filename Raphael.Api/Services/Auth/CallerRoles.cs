using Microsoft.Extensions.Options;
using Raphael.Notification.Infrastructure.Realtime;
using System.Security.Claims;

namespace Raphael.Api.Services.Auth
{
    /// <summary>Which kind of caller a token belongs to, decided the same way the notification hub does.</summary>
    public interface ICallerRoles
    {
        /// <summary>A staff member or driver id, from the <c>UserId</c> claim only.</summary>
        int? UserIdOf(ClaimsPrincipal user);

        int? ProviderIdOf(ClaimsPrincipal user);

        /// <summary>Dispatch office staff: not a driver, patient, integration or integrator-bound user.</summary>
        bool IsOffice(ClaimsPrincipal user);
    }

    /// <inheritdoc />
    /// <remarks>
    /// ⚠️ Never falls back to <c>sub</c>/NameIdentifier: for a patient it is a CustomerId and for an
    /// integration an IntegratorId, both of which would pass for a user id. With
    /// <c>DriverRoleIds</c> empty nobody is office, the same least-privilege default as the hub.
    /// </remarks>
    public sealed class CallerRoles : ICallerRoles
    {
        private readonly NotificationRealtimeOptions _options;

        public CallerRoles(IOptions<NotificationRealtimeOptions> options)
        {
            _options = options.Value;
        }

        public int? UserIdOf(ClaimsPrincipal user) => IntClaim(user, "UserId");

        public int? ProviderIdOf(ClaimsPrincipal user) => IntClaim(user, "UserProviderId");

        public bool IsOffice(ClaimsPrincipal user) =>
            UserIdOf(user).HasValue
            && !HasValue(user, "CustomerId")
            && !HasValue(user, "IntegratorId")
            && !HasValue(user, "UserIntegratorId")
            && _options.DriverRoleIds.Length > 0
            && !HasDriverRole(user);

        private bool HasDriverRole(ClaimsPrincipal user) =>
            user.FindAll(ClaimTypes.Role)
                .Concat(user.FindAll("Role"))
                .Any(claim =>
                    int.TryParse(claim.Value, out var roleId) &&
                    _options.DriverRoleIds.Contains(roleId));

        private static int? IntClaim(ClaimsPrincipal user, string type) =>
            int.TryParse(user.FindFirst(type)?.Value, out var value) && value > 0 ? value : null;

        private static bool HasValue(ClaimsPrincipal user, string type) =>
            !string.IsNullOrWhiteSpace(user.FindFirst(type)?.Value);
    }
}
