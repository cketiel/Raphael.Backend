using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Raphael.Api.Settings;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Raphael.Api.Services.Notifications
{
    /// <summary>
    /// Issues the short lived token an integration uses to open the notification hub.
    /// </summary>
    /// <remarks>
    /// SignalR carries its credential in the query string when the transport is not
    /// WebSockets, and a query string ends up in access logs and in every proxy along the
    /// way. An API Key is a long lived credential that opens the whole integration
    /// surface, so it must never travel that way. The integration exchanges it, over a
    /// header, for a token that only opens the hub and expires the same day.
    /// </remarks>
    public class IntegrationHubTokenService : IIntegrationHubTokenService
    {
        private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

        private readonly JwtSettings _jwtSettings;

        public IntegrationHubTokenService(IOptions<JwtSettings> jwtOptions)
        {
            _jwtSettings = jwtOptions.Value;
        }

        public IntegrationHubToken Issue(int integratorId, string? integratorName)
        {
            var expiresAtUtc = DateTime.UtcNow.Add(Lifetime);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, integratorId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, integratorName ?? $"Integrator {integratorId}"),
                new Claim("IntegratorId", integratorId.ToString()),
                new Claim(ClaimTypes.Role, "Integration")
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Key));

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: expiresAtUtc,
                signingCredentials: new SigningCredentials(
                    key,
                    SecurityAlgorithms.HmacSha256));

            return new IntegrationHubToken(
                new JwtSecurityTokenHandler().WriteToken(token),
                expiresAtUtc);
        }
    }
}
