namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// What <c>POST /api/Auth/login</c> answers with.
    /// </summary>
    /// <remarks>
    /// <para>
    /// ⚠️ This replaced an anonymous object, and every property below is named and typed to
    /// serialise to <b>exactly</b> what that object produced — including <c>userId</c> and
    /// <c>role</c> being strings rather than numbers, which is what the clients' own
    /// <c>LoginResponse</c> classes declare. An anonymous object is not a contract anybody can
    /// mirror, which is how three applications came to hold three guesses at this shape.
    /// </para>
    /// <para>
    /// The three token fields at the bottom are <b>additive</b>. A client that does not know
    /// about them deserialises the response into its own class and ignores them, which is why
    /// Desktop 1.8.1 and Driver 1.4.0 keep working untouched against this.
    /// </para>
    /// </remarks>
    public class LoginResponseDto
    {
        /// <summary>The access token. Sent on every request as <c>Authorization: Bearer</c>.</summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>A string, not a number. Kept that way because the clients declare it so.</summary>
        public string UserId { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        /// <summary>The role id, as a string. Same reason as <see cref="UserId"/>.</summary>
        public string Role { get; set; } = string.Empty;

        public int? IntegratorId { get; set; }

        public int? ProviderId { get; set; }

        public bool IsSuccess { get; set; }

        // ----------------------------------------------------------- added 2026-09-19

        /// <summary>
        /// When <see cref="Token"/> stops being accepted. Given plainly so a client does not
        /// have to decode the JWT to find out.
        /// </summary>
        public DateTime AccessTokenExpiresAtUtc { get; set; }

        /// <summary>
        /// Opaque, single-use, and the only thing that can buy a new access token. Stored by
        /// the client where a credential belongs — DPAPI on Windows, SecureStorage on the
        /// phone — never beside the executable.
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// When the refresh token dies if it goes unused. Every use pushes it forward, up to a
        /// ceiling the client is not told about because it cannot do anything with it.
        /// </summary>
        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
