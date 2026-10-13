using Microsoft.AspNetCore.Authorization;

namespace Raphael.Api.Services.Auth
{
    /// <summary>Named authorisation policies, so a rule is written once and not as a magic number.</summary>
    public static class AuthPolicies
    {
        /// <summary>
        /// The platform's administrator: role 1 belonging to no Provider and no Integrator, that is
        /// Raphael's own office (sales/02-product-model.md §2, decision M2).
        /// </summary>
        /// <remarks>
        /// Role 1 alone is not enough once Raphael is sold: a client company's admin is also role 1,
        /// with its <c>ProviderId</c>, and a clinic's admin is role 1 with its <c>IntegratorId</c>.
        /// Neither may create tenants, edit the catalogs, read every integrator's API key or touch
        /// Google Maps and the notification catalog. Today this gives exactly what <c>Roles = "1"</c>
        /// gave, because no office admin has a Provider (checked by the user, 2026-10-12) and clinic
        /// admins were already kept out by <c>[NotForClinicUsers]</c>.
        /// </remarks>
        public const string PlatformAdmin = "PlatformAdmin";

        private const string AdminRoleId = "1";

        public static void Add(AuthorizationOptions options)
        {
            options.AddPolicy(PlatformAdmin, policy => policy
                .RequireRole(AdminRoleId)
                .RequireAssertion(context =>
                    string.IsNullOrEmpty(context.User.FindFirst("UserProviderId")?.Value)
                    && string.IsNullOrEmpty(context.User.FindFirst("UserIntegratorId")?.Value)));
        }
    }
}
