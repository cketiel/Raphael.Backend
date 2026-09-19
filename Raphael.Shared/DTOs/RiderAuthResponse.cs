namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// What <c>POST /api/Rider/auth/identify</c> answers with.
    /// </summary>
    /// <remarks>
    /// The three token fields are <b>additive</b>, added 2026-09-19 alongside refresh tokens.
    /// The patient app is pre-release, so nothing in the field depends on this shape yet.
    /// </remarks>
    public class RiderAuthResponse
    {
        public string Token { get; set; } = string.Empty;
        public CustomerResponseDto Customer { get; set; }
        public bool IsSuccess { get; set; }

        // ----------------------------------------------------------- added 2026-09-19

        /// <summary>
        /// When <see cref="Token"/> stops being accepted.
        /// </summary>
        /// <remarks>
        /// ⚠️ Until the patient app learns to renew, this is still a year away — the token has
        /// carried <c>AddDays(365)</c> since it was written, commented "sesión persistente".
        /// That is the opposite problem to the one the office and driver apps have: not a
        /// session that ends too soon, but one that cannot be ended at all. A lost or resold
        /// phone keeps opening that patient's trips for a year, and the only revocation
        /// available was rotating the signing key, which signs out everybody. Shortening it is
        /// a change the client has to be ready for, so it happens with the client, not here.
        /// </remarks>
        public DateTime AccessTokenExpiresAtUtc { get; set; }

        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiresAtUtc { get; set; }
    }
}
