using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Raphael.Api.Settings;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services.Auth
{
    /// <summary>
    /// The one place tokens are minted. Before this existed the JWT was built inline in
    /// <c>AuthController</c> and again in <c>RiderService</c>, with different lifetimes and no
    /// way to take either back.
    /// </summary>
    public sealed class AuthTokenService : IAuthTokenService
    {
        /// <summary>
        /// How long after a rotation a repeat of the old token is still read as a retry rather
        /// than as theft.
        /// </summary>
        /// <remarks>
        /// ⚠️ Without this window, reuse detection punishes bad signal. A driver in a dead spot
        /// sends a refresh, the reply never arrives, the phone retries, and the second attempt
        /// presents a token the server has already rotated — which looks exactly like a stolen
        /// credential and would revoke the whole family, dropping them to the login screen in
        /// the middle of a route. Real theft does not usually replay a token within seconds of
        /// the legitimate holder, so the window costs very little detection and buys back the
        /// failure mode that would actually have happened every week.
        /// </remarks>
        private const int RotationGraceSeconds = 30;

        private readonly RaphaelContext _context;
        private readonly IOptionsMonitor<JwtSettings> _jwt;
        private readonly IOptionsMonitor<SessionPolicyOptions> _policies;
        private readonly ILogger<AuthTokenService> _logger;

        public AuthTokenService(
            RaphaelContext context,
            IOptionsMonitor<JwtSettings> jwt,
            IOptionsMonitor<SessionPolicyOptions> policies,
            ILogger<AuthTokenService> logger)
        {
            _context = context;
            _jwt = jwt;
            _policies = policies;
            _logger = logger;
        }

        // ------------------------------------------------------------------ issuing

        public async Task<IssuedTokens> IssueForUserAsync(
            User user,
            string? clientApp,
            CancellationToken cancellationToken = default)
        {
            var policy = _policies.CurrentValue.For(clientApp);
            var now = DateTime.UtcNow;

            var access = CreateAccessToken(ClaimsForUser(user), policy, now, out var accessExpiry);

            var refresh = await CreateRefreshTokenAsync(
                userId: user.Id,
                customerId: null,
                clientApp: clientApp,
                familyId: Guid.NewGuid(),
                absoluteExpiresAtUtc: now.AddHours(policy.RefreshAbsoluteHours),
                policy: policy,
                now: now,
                cancellationToken: cancellationToken);

            return new IssuedTokens(access, accessExpiry, refresh.Token, refresh.Row.ExpiresAtUtc);
        }

        public async Task<IssuedTokens> IssueForCustomerAsync(
            Customer customer,
            string? clientApp,
            CancellationToken cancellationToken = default)
        {
            var policy = _policies.CurrentValue.For(clientApp);
            var now = DateTime.UtcNow;

            var access = CreateAccessToken(ClaimsForCustomer(customer), policy, now, out var accessExpiry);

            var refresh = await CreateRefreshTokenAsync(
                userId: null,
                customerId: customer.Id,
                clientApp: clientApp,
                familyId: Guid.NewGuid(),
                absoluteExpiresAtUtc: now.AddHours(policy.RefreshAbsoluteHours),
                policy: policy,
                now: now,
                cancellationToken: cancellationToken);

            return new IssuedTokens(access, accessExpiry, refresh.Token, refresh.Row.ExpiresAtUtc);
        }

        // ------------------------------------------------------------------ rotating

        public async Task<RefreshOutcome> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return RefreshOutcome.Failed(RefreshFailure.Unknown);
            }

            var hash = Hash(refreshToken);
            var now = DateTime.UtcNow;

            var row = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

            if (row is null)
            {
                return RefreshOutcome.Failed(RefreshFailure.Unknown);
            }

            // Already rotated. Either this client retrying, or somebody else holding a copy.
            if (row.ReplacedByTokenHash is not null)
            {
                var since = now - (row.RevokedAtUtc ?? row.CreatedAtUtc);

                if (since.TotalSeconds <= RotationGraceSeconds)
                {
                    return RefreshOutcome.Failed(RefreshFailure.RotatedRecently);
                }

                await RevokeFamilyAsync(row.FamilyId, "reuse-detected", now, cancellationToken);

                // Logged as a warning, not an error: it is a real signal and it is also what a
                // phone restored from a backup looks like. No identifier of the person goes in.
                _logger.LogWarning(
                    "Refresh token reuse detected for family {FamilyId} ({ClientApp}); the whole " +
                    "family has been revoked. Last rotation was {Minutes:F0} minutes ago.",
                    row.FamilyId,
                    row.ClientApp,
                    since.TotalMinutes);

                return RefreshOutcome.Failed(RefreshFailure.ReuseDetected);
            }

            if (!row.IsActive(now))
            {
                return RefreshOutcome.Failed(RefreshFailure.Expired);
            }

            // The policy comes from the row, not from this request's header: it was decided at
            // sign-in and a later caller does not get to ask for a longer session.
            var policy = _policies.CurrentValue.For(row.ClientApp);

            Claim[] claims;

            if (row.UserId is int userId)
            {
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

                // Deactivating an account has to end its sessions, or it only stops the next
                // sign-in and leaves whoever is already inside there for weeks.
                if (user is null || !user.IsActive)
                {
                    await RevokeFamilyAsync(row.FamilyId, "subject-unavailable", now, cancellationToken);
                    return RefreshOutcome.Failed(RefreshFailure.SubjectUnavailable);
                }

                claims = ClaimsForUser(user);
            }
            else
            {
                var customer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.Id == row.CustomerId, cancellationToken);

                if (customer is null)
                {
                    await RevokeFamilyAsync(row.FamilyId, "subject-unavailable", now, cancellationToken);
                    return RefreshOutcome.Failed(RefreshFailure.SubjectUnavailable);
                }

                claims = ClaimsForCustomer(customer);
            }

            var access = CreateAccessToken(claims, policy, now, out var accessExpiry);

            // ⚠️ The absolute ceiling is inherited, never recalculated. Recomputing it here
            // would let a session renew itself forever, which is the whole thing the ceiling
            // exists to prevent.
            var issued = await CreateRefreshTokenAsync(
                userId: row.UserId,
                customerId: row.CustomerId,
                clientApp: row.ClientApp,
                familyId: row.FamilyId,
                absoluteExpiresAtUtc: row.AbsoluteExpiresAtUtc,
                policy: policy,
                now: now,
                cancellationToken: cancellationToken,
                saveChanges: false);

            row.RevokedAtUtc = now;
            row.RevokedReason = "rotated";
            row.ReplacedByTokenHash = issued.Row.TokenHash;
            row.LastUsedAtUtc = now;

            await _context.SaveChangesAsync(cancellationToken);

            return RefreshOutcome.Success(
                new IssuedTokens(access, accessExpiry, issued.Token, issued.Row.ExpiresAtUtc));
        }

        public async Task<bool> RevokeAsync(
            string refreshToken,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return true;
            }

            var hash = Hash(refreshToken);

            var row = await _context.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);

            if (row is null || row.RevokedAtUtc is not null)
            {
                return true;
            }

            // The family, not the row. Signing out means this device's session is over, and
            // the row is only the newest link in a chain that all belongs to that one sign-in.
            await RevokeFamilyAsync(row.FamilyId, "signed-out", DateTime.UtcNow, cancellationToken);

            return true;
        }

        // ------------------------------------------------------------------ internals

        private sealed record NewRefreshToken(string Token, RefreshToken Row);

        private async Task<NewRefreshToken> CreateRefreshTokenAsync(
            int? userId,
            int? customerId,
            string? clientApp,
            Guid familyId,
            DateTime absoluteExpiresAtUtc,
            SessionPolicy policy,
            DateTime now,
            CancellationToken cancellationToken,
            bool saveChanges = true)
        {
            var token = CreateOpaqueToken();

            // The sliding window cannot outlive the ceiling; whichever comes first wins.
            var sliding = now.AddMinutes(policy.RefreshSlidingMinutes);
            var expires = sliding < absoluteExpiresAtUtc ? sliding : absoluteExpiresAtUtc;

            var row = new RefreshToken
            {
                UserId = userId,
                CustomerId = customerId,
                TokenHash = Hash(token),
                FamilyId = familyId,
                ClientApp = string.IsNullOrWhiteSpace(clientApp) ? "Unknown" : clientApp,
                CreatedAtUtc = now,
                ExpiresAtUtc = expires,
                AbsoluteExpiresAtUtc = absoluteExpiresAtUtc
            };

            _context.RefreshTokens.Add(row);

            if (saveChanges)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return new NewRefreshToken(token, row);
        }

        private async Task RevokeFamilyAsync(
            Guid familyId,
            string reason,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var family = await _context.RefreshTokens
                .Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var member in family)
            {
                member.RevokedAtUtc = now;
                member.RevokedReason = reason;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>256 bits of randomness, URL-safe so it survives any transport.</summary>
        private static string CreateOpaqueToken() =>
            Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32));

        /// <summary>
        /// What goes in the database. See <see cref="RefreshToken.TokenHash"/> for why plain
        /// SHA-256 is correct for a value with this much entropy.
        /// </summary>
        private static string Hash(string token) =>
            Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private string CreateAccessToken(
            Claim[] claims,
            SessionPolicy policy,
            DateTime now,
            out DateTime expiresAtUtc)
        {
            var settings = _jwt.CurrentValue;

            // ⚠️ The access token's lifetime comes from the session policy. Jwt:ExpiresInMinutes
            // is no longer read by anything -- which is a trap worth naming, because the clients
            // already in the field send no X-Client-App and therefore land on SessionPolicy
            // "Default". That entry carries 600 for exactly this reason: it is the old value,
            // kept so Desktop 1.8.1 and Driver 1.4.0 behave precisely as they do today. Setting
            // Default to something sensible-looking like 60 would not tighten anything for them,
            // it would throw them out ten times more often with no way to renew.
            expiresAtUtc = now.AddMinutes(policy.AccessTokenMinutes);

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: settings.Issuer,
                audience: settings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// ⚠️ Reproduced exactly as <c>AuthController.Login</c> built them. Authorisation all
        /// over the API reads these by name; dropping or renaming one does not fail a build, it
        /// fails a dispatcher's screen at runtime.
        /// </summary>
        private static Claim[] ClaimsForUser(User user) => new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.RoleId.ToString()),
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            new Claim("UserId", user.Id.ToString()),
            new Claim("Username", user.Username),
            new Claim("Role", user.RoleId.ToString()),
            new Claim("UserIntegratorId", user.IntegratorId?.ToString() ?? ""),
            new Claim("UserProviderId", user.ProviderId?.ToString() ?? "")
        };

        /// <summary>
        /// ⚠️ Reproduced exactly as <c>RiderService.GenerateRiderToken</c> built them.
        /// </summary>
        /// <remarks>
        /// Worth naming rather than quietly carrying forward: <c>unique_name</c> and
        /// <c>CustomerName</c> put a patient's full name inside the token. A JWT is base64, not
        /// encryption, and this one is stored on a phone. Changing it means changing every
        /// reader of those claims, so it is not changed here — it is written down.
        /// </remarks>
        private static Claim[] ClaimsForCustomer(Customer customer) => new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, customer.FullName),
            new Claim("CustomerId", customer.Id.ToString()),
            new Claim("CustomerName", customer.FullName),
            new Claim(ClaimTypes.Role, "Rider")
        };
    }
}
