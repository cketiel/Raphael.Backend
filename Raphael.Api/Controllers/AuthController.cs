using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Raphael.Api.Services.Auth;
using Raphael.Api.Settings;
using Raphael.Api.Versioning;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Helpers;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly RaphaelContext _context;
        private readonly IAuthTokenService _tokens;
        private readonly IOptionsMonitor<AppAccessOptions> _appAccess;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            RaphaelContext context,
            IAuthTokenService tokens,
            IOptionsMonitor<AppAccessOptions> appAccess,
            ILogger<AuthController> logger)
        {
            _context = context;
            _tokens = tokens;
            _appAccess = appAccess;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !PasswordHasher.Verify(request.Password, user!.PasswordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            if (!user.IsActive)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { code = "account_disabled", message = "User account is disabled. Contact administrator." });
            }

            // A clinic user signs in on behalf of its Integrator. When the office switches the
            // Integrator off, the API key already stops working; its people must stop too.
            if (user.IntegratorId is int integratorId && !await IntegratorIsActiveAsync(integratorId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { code = "integrator_disabled", message = "Your organization's access is disabled. Contact the office." });
            }

            // RE-055: the same for the users of a Provider the platform switched off. Checked here
            // and at renewal only, never on every request (user decision, 2026-10-18).
            if (user.ProviderId is int deactivatedProviderId && await ProviderIsDeactivatedAsync(deactivatedProviderId))
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { code = "provider_disabled", message = "Your company's access is disabled. Contact the office." });
            }

            // A Provider that works outside Raphael only sees its trips in the Booking portal. Its
            // users are created by clinics too, and must never be treated as the office: not in
            // the Desktop, not in any other app (PROVIDER_ACCESS_MODEL.md §3.4, F15).
            var isExternalProvider = user.ProviderId is int providerId
                && await _context.Providers.AnyAsync(p => p.Id == providerId && p.WorkMode == ProviderWorkMode.External);
            if (isExternalProvider && ClientApp() != PortalClaims.BookingApp)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    new { code = "wrong_app", message = "This account only has access to the Booking portal." });
            }

            // RE-048: every user signs in only to its own application (ClientApps.AllowedFor).
            // The rule above is the first case of this one and stays on whatever the mode.
            var mode = _appAccess.CurrentValue.Mode;
            if (mode != AppAccessMode.Off)
            {
                var app = ClientApps.Normalize(ClientApp());
                var allowed = ClientApps.AllowedFor(user, isExternalProvider ? ProviderWorkMode.External : null);
                if (!allowed.Contains(app))
                {
                    // Role and application only: no name, no username.
                    _logger.LogWarning(
                        "App access {Outcome}: sign-in of a role {Role} user from {App}, which only admits {Allowed}.",
                        mode == AppAccessMode.Enforce ? "refused" : "would refuse",
                        user.RoleId,
                        app,
                        string.Join(",", allowed));

                    if (mode == AppAccessMode.Enforce)
                    {
                        return StatusCode(
                            StatusCodes.Status403Forbidden,
                            new { code = "wrong_app", message = "This account cannot sign in to this application." });
                    }
                }
            }

            // Which application this is decides how long the session lasts, and it is recorded
            // on the refresh token here rather than re-read later. See SessionPolicyOptions.
            var issued = await _tokens.IssueForUserAsync(user, ClientApp());

            return Ok(new LoginResponseDto
            {
                Token = issued.AccessToken,
                UserId = user.Id.ToString(),
                Username = user.Username,
                Role = user.RoleId.ToString(),
                IntegratorId = user.IntegratorId,
                ProviderId = user.ProviderId,
                IsExternalProvider = isExternalProvider,
                MustChangePassword = user.MustChangePassword,
                IsSuccess = true,
                AccessTokenExpiresAtUtc = issued.AccessTokenExpiresAtUtc,
                RefreshToken = issued.RefreshToken,
                RefreshTokenExpiresAtUtc = issued.RefreshTokenExpiresAtUtc
            });
        }

        /// <summary>
        /// Exchanges a refresh token for a new pair.
        /// </summary>
        /// <remarks>
        /// ⚠️ Anonymous by necessity, not by oversight: the access token is expired by
        /// definition when this is called, so requiring one would make the endpoint useless.
        /// The refresh token in the body is the credential.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(AuthTokenPairDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            var outcome = await _tokens.RefreshAsync(request.RefreshToken);

            if (outcome.Succeeded)
            {
                var t = outcome.Tokens!;

                return Ok(new AuthTokenPairDto
                {
                    Token = t.AccessToken,
                    AccessTokenExpiresAtUtc = t.AccessTokenExpiresAtUtc,
                    RefreshToken = t.RefreshToken,
                    RefreshTokenExpiresAtUtc = t.RefreshTokenExpiresAtUtc
                });
            }

            // 409 rather than 401 for the retry case, and the distinction earns its keep: 401
            // tells a client to send the user back to the login screen, and this is the one
            // failure where it must not. It means "you already renewed; use what you have".
            if (outcome.Failure == RefreshFailure.RotatedRecently)
            {
                return Conflict(new
                {
                    error = "rotated_recently",
                    message = "This refresh token has just been rotated. Use the one you were given."
                });
            }

            // Everything else is one answer on purpose. Telling a caller whether a token was
            // unknown, expired or revoked for reuse is telling an attacker which of their
            // guesses was closest.
            return Unauthorized(new
            {
                error = "invalid_refresh_token",
                message = "Sign in again."
            });
        }

        /// <summary>
        /// Ends the session the refresh token belongs to.
        /// </summary>
        /// <remarks>
        /// ⚠️ Anonymous on purpose. Holding the refresh token is the authority to revoke it —
        /// there is no privilege to escalate in throwing your own credential away. Requiring a
        /// valid access token would mean that signing out stops working at exactly the moment
        /// it matters most: when the session has already expired and the user is trying to
        /// leave a shared machine clean.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
        {
            await _tokens.RevokeAsync(request.RefreshToken);

            // Always 204, even for a token that was unknown or already dead. The caller's goal
            // is "this must not work any more", which is true either way, and answering
            // differently would turn sign-out into an oracle for guessing valid tokens.
            return NoContent();
        }

        /// <summary>What the client said it is, for the session policy. May be absent.</summary>
        private string? ClientApp() =>
            Request.Headers.TryGetValue(ClientVersionHeaders.App, out var value)
                ? value.ToString()
                : null;

        private Task<bool> IntegratorIsActiveAsync(int integratorId) =>
            _context.Integrators.AnyAsync(i => i.Id == integratorId && i.IsActive);

        /// <summary>
        /// Asks whether the Provider is switched off, not whether it is on: a row the query cannot
        /// see must never lock its users out.
        /// </summary>
        private Task<bool> ProviderIsDeactivatedAsync(int providerId) =>
            _context.Providers.IgnoreQueryFilters().AnyAsync(p => p.Id == providerId && !p.IsActive);
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
