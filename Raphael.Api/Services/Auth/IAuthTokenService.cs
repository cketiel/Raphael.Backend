using Raphael.Shared.Entities;

namespace Raphael.Api.Services.Auth
{
    /// <summary>
    /// Issues and rotates the pair of tokens a signed-in client holds: a short-lived access
    /// token it sends on every request, and a long-lived refresh token it only ever sends here.
    /// </summary>
    public interface IAuthTokenService
    {
        /// <summary>Signs in a member of staff or a driver.</summary>
        Task<IssuedTokens> IssueForUserAsync(
            User user,
            string? clientApp,
            CancellationToken cancellationToken = default);

        /// <summary>Signs in a patient.</summary>
        Task<IssuedTokens> IssueForCustomerAsync(
            Customer customer,
            string? clientApp,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Exchanges a refresh token for a new pair, revoking the one presented.
        /// </summary>
        Task<RefreshOutcome> RefreshAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ends the session the token belongs to. Idempotent: revoking something already
        /// revoked, expired or unknown is reported as success, because the caller's goal —
        /// "this must not work any more" — is satisfied either way.
        /// </summary>
        Task<bool> RevokeAsync(
            string refreshToken,
            CancellationToken cancellationToken = default);
    }

    /// <summary>A freshly issued pair, and when each half stops working.</summary>
    /// <remarks>
    /// The expiry times are handed to the client rather than left for it to decode out of the
    /// JWT. A client that has to parse a token to know when to renew it ends up with its own
    /// half-correct JWT parser, which is how Desktop got one.
    /// </remarks>
    public sealed record IssuedTokens(
        string AccessToken,
        DateTime AccessTokenExpiresAtUtc,
        string RefreshToken,
        DateTime RefreshTokenExpiresAtUtc);

    /// <summary>Why a refresh did not produce a new pair.</summary>
    public enum RefreshFailure
    {
        None = 0,

        /// <summary>No row for that token. Either invented, or from a database that is gone.</summary>
        Unknown,

        /// <summary>Past its inactivity window or its absolute ceiling.</summary>
        Expired,

        /// <summary>
        /// Already rotated, long enough ago that a retry does not explain it. Two parties hold
        /// the same credential; the whole family has been revoked.
        /// </summary>
        ReuseDetected,

        /// <summary>
        /// Already rotated, moments ago. Almost certainly this client retrying after a dropped
        /// response rather than anybody stealing anything, so nothing is revoked and the client
        /// is told to use the token it already has.
        /// </summary>
        RotatedRecently,

        /// <summary>The row is fine but the person it points at is gone or deactivated.</summary>
        SubjectUnavailable
    }

    /// <summary>The result of an exchange: either a new pair, or the reason there is none.</summary>
    public sealed record RefreshOutcome(IssuedTokens? Tokens, RefreshFailure Failure)
    {
        public bool Succeeded => Tokens is not null;

        public static RefreshOutcome Success(IssuedTokens tokens) => new(tokens, RefreshFailure.None);

        public static RefreshOutcome Failed(RefreshFailure failure) => new(null, failure);
    }
}
