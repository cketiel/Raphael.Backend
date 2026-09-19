namespace Raphael.Api.Settings
{
    /// <summary>
    /// How long a session lasts, per client application.
    /// </summary>
    /// <remarks>
    /// <para>
    /// One number for everybody would have been simpler and wrong. A driver's phone is a
    /// personal device carried through a shift, and asking for a password again mid-route is
    /// the kind of friction that ends with the password written on the dashboard. A dispatch
    /// workstation is a shared machine in an office that can be left alone with a patient's
    /// name, address and phone on screen. Those two want opposite answers.
    /// </para>
    /// <para>
    /// The values live in configuration rather than in code so they can be tightened without a
    /// deployment — a compliance review is exactly the sort of thing that changes them.
    /// </para>
    /// <para>
    /// ⚠️ In Azure these bind as <c>SessionPolicy__Apps__Driver__RefreshAbsoluteHours</c>.
    /// Unlike the CORS origins, this is a dictionary keyed by name rather than an array keyed
    /// by index, so an App Setting replaces the entry it names and cannot silently shift the
    /// others.
    /// </para>
    /// </remarks>
    public sealed class SessionPolicyOptions
    {
        public const string SectionName = "SessionPolicy";

        /// <summary>
        /// Applied when the caller sends no <c>X-Client-App</c>, or one nobody has configured.
        /// Deliberately the strictest of the set: an unknown client gets the short session.
        /// </summary>
        public SessionPolicy Default { get; set; } = new();

        /// <summary>
        /// Keyed by what the application sends in <c>X-Client-App</c>: <c>Desktop</c>,
        /// <c>Driver</c>, <c>Rider</c>. Matched without regard to case, like
        /// <see cref="Versioning.ClientCompatibilityOptions"/>.
        /// </summary>
        public Dictionary<string, SessionPolicy> Apps { get; set; } = new();

        /// <summary>
        /// The policy for a client, falling back to <see cref="Default"/>.
        /// </summary>
        public SessionPolicy For(string? clientApp)
        {
            if (string.IsNullOrWhiteSpace(clientApp))
            {
                return Default;
            }

            foreach (var pair in Apps)
            {
                if (string.Equals(pair.Key, clientApp, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value ?? Default;
                }
            }

            return Default;
        }
    }

    /// <summary>One application's session durations.</summary>
    public sealed class SessionPolicy
    {
        /// <summary>
        /// Lifetime of the JWT. Short is the point: it is the window in which a stolen access
        /// token is still worth something, and nothing can shorten it after it is issued.
        /// </summary>
        public int AccessTokenMinutes { get; set; } = 60;

        /// <summary>
        /// Inactivity window. The refresh token dies if it goes unused this long; every use
        /// pushes it forward.
        /// </summary>
        public int RefreshSlidingMinutes { get; set; } = 60;

        /// <summary>
        /// Hard ceiling from sign-in, whatever the activity. This is the one that guarantees a
        /// password is typed again eventually.
        /// </summary>
        /// <remarks>
        /// In hours rather than days because the office needs half of one: a dispatch
        /// workstation gets a working day, a driver's phone gets three months.
        /// </remarks>
        public int RefreshAbsoluteHours { get; set; } = 24;

        /// <summary>
        /// Rejects a policy that cannot mean anything, so a typo in configuration fails at
        /// startup instead of minting a session that expires before it is returned — or one
        /// that never expires at all.
        /// </summary>
        public void Validate(string name)
        {
            if (AccessTokenMinutes <= 0)
            {
                throw new InvalidOperationException(
                    $"SessionPolicy '{name}': AccessTokenMinutes must be greater than zero.");
            }

            if (RefreshSlidingMinutes <= 0)
            {
                throw new InvalidOperationException(
                    $"SessionPolicy '{name}': RefreshSlidingMinutes must be greater than zero.");
            }

            if (RefreshAbsoluteHours <= 0)
            {
                throw new InvalidOperationException(
                    $"SessionPolicy '{name}': RefreshAbsoluteHours must be greater than zero.");
            }

            // A sliding window longer than the ceiling is not wrong, only pointless -- the
            // ceiling wins. Saying so beats leaving somebody to work out why raising the
            // sliding window changed nothing.
            if (RefreshSlidingMinutes > RefreshAbsoluteHours * 60)
            {
                throw new InvalidOperationException(
                    $"SessionPolicy '{name}': RefreshSlidingMinutes ({RefreshSlidingMinutes}) " +
                    $"exceeds RefreshAbsoluteHours ({RefreshAbsoluteHours}), so the inactivity " +
                    "window can never be reached. Raise the ceiling or lower the window.");
            }
        }
    }
}
