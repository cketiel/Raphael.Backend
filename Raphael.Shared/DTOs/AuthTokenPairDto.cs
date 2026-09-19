namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// What <c>POST /api/Auth/refresh</c> answers with: a new access token and the refresh
    /// token that replaces the one just spent.
    /// </summary>
    /// <remarks>
    /// ⚠️ The refresh token is single-use. The client must store the one returned here and
    /// discard the one it sent, or its next renewal presents a token the server has already
    /// rotated. Done once, that reads as a retry and is forgiven; done repeatedly it is
    /// indistinguishable from a stolen credential and ends the session.
    /// </remarks>
    public class AuthTokenPairDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime AccessTokenExpiresAtUtc { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
