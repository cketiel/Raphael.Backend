using System.Text.Json.Serialization;

namespace Raphael.Shared.Entities
{
    /// <summary>
    /// One long-lived credential that buys a short-lived access token, and the server-side
    /// record that makes signing out mean something.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Before this table there was no session on the server at all: a JWT was minted at login
    /// and nothing could take it back before it expired. That is why rotating the signing key
    /// was the only revocation available, and why doing it throws every user out at once.
    /// </para>
    /// <para>
    /// ⚠️ <b>Two kinds of subject, exactly one per row.</b> The ecosystem issues tokens down two
    /// different paths: staff and drivers sign in through <c>POST /api/Auth/login</c> and are
    /// rows in <c>Users</c>; patients identify through <c>POST /api/Rider/auth/identify</c> and
    /// are rows in <c>Customers</c>. Both get refresh tokens, so both foreign keys are here and
    /// the one that does not apply stays null. Modelling it as an untyped subject id would have
    /// been simpler and would have left a deleted patient's sessions alive.
    /// </para>
    /// </remarks>
    public class RefreshToken
    {
        public long Id { get; set; }

        /// <summary>
        /// The staff or driver this session belongs to. Null when <see cref="CustomerId"/> is set.
        /// </summary>
        public int? UserId { get; set; }

        [JsonIgnore]
        public User? User { get; set; }

        /// <summary>
        /// The patient this session belongs to. Null when <see cref="UserId"/> is set.
        /// </summary>
        public int? CustomerId { get; set; }

        [JsonIgnore]
        public Customer? Customer { get; set; }

        /// <summary>
        /// Base64 of the SHA-256 of the token. <b>The token itself is never stored.</b>
        /// </summary>
        /// <remarks>
        /// ⚠️ Plain SHA-256 is the right choice here and is not an oversight. The token is 256
        /// bits of cryptographically secure randomness, so it cannot be guessed and there is
        /// nothing for a slow KDF to slow down. bcrypt and PBKDF2 exist to punish guessing
        /// low-entropy passwords — which is exactly why <c>PasswordHasher</c> hashing passwords
        /// with unsalted SHA-256 is a real finding and this is not the same thing.
        /// </remarks>
        [JsonIgnore]
        public string TokenHash { get; set; } = string.Empty;

        /// <summary>
        /// Every token descended from one sign-in shares this. Rotation replaces the row and
        /// keeps the family.
        /// </summary>
        /// <remarks>
        /// This is what makes theft detectable. A refresh token is used once and replaced; if a
        /// token that has already been replaced turns up again, two parties hold the same
        /// credential and one of them should not. There is no way to tell which, so the whole
        /// family is revoked and both are sent back to the sign-in screen.
        /// </remarks>
        public Guid FamilyId { get; set; }

        /// <summary>
        /// Which application asked for it: <c>Desktop</c>, <c>Driver</c> or <c>Rider</c>. Taken
        /// from <c>X-Client-App</c> at sign-in and never changed afterwards.
        /// </summary>
        /// <remarks>
        /// It selects the session policy, and it is recorded rather than re-read so that a later
        /// request cannot claim a different one. ⚠️ The header is client-supplied and
        /// unauthenticated: an application that lies about it gets a different session
        /// <i>duration</i>, never more authority. That is an accepted, bounded weakness.
        /// </remarks>
        public string ClientApp { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>
        /// End of the inactivity window. Moves forward every time the token is used.
        /// </summary>
        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>
        /// Hard ceiling. Never moves, whatever the activity: the point at which the user types a
        /// password again no matter how continuously they have been working.
        /// </summary>
        public DateTime AbsoluteExpiresAtUtc { get; set; }

        public DateTime? LastUsedAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>Hash of the token that replaced this one, when it was rotated.</summary>
        [JsonIgnore]
        public string? ReplacedByTokenHash { get; set; }

        /// <summary>
        /// Why it was revoked — <c>rotated</c>, <c>signed-out</c>, <c>reuse-detected</c>. Short,
        /// and never carries anything about the person.
        /// </summary>
        public string? RevokedReason { get; set; }

        /// <summary>
        /// Usable right now: not revoked, and inside both the sliding and the absolute window.
        /// </summary>
        public bool IsActive(DateTime utcNow) =>
            RevokedAtUtc == null &&
            utcNow < ExpiresAtUtc &&
            utcNow < AbsoluteExpiresAtUtc;
    }
}
