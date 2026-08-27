namespace Raphael.Shared.Definitions.Notifications;

/// <summary>
/// How long a notification stays visible and how long its row survives afterwards.
/// </summary>
/// <remarks>
/// Without this the Notifications table only grows. Every completed trip, every arrival
/// and every cancellation would sit there forever, and the inbox of a patient who has
/// been travelling for two years would take a full table scan to open.
///
/// <para>
/// The policy lives in code rather than in the database on purpose: it is a business
/// decision that must be reviewed alongside the events it applies to, and it is
/// documented in <c>_meta/NOTIFICATIONS_RETENTION.md</c>.
/// </para>
/// </remarks>
public static class NotificationRetentionPolicy
{
    /// <summary>
    /// How long a notification is worth reading.
    /// </summary>
    /// <remarks>
    /// Office and driver notices are operational: they say "something changed, refresh".
    /// Once the shift is over they help nobody. What a patient receives is about their
    /// own care and deserves to stay a while, and an integration needs room to reconcile.
    /// </remarks>
    public static TimeSpan VisibleFor(RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        if (recipientType.Id == RecipientType.DesktopUser.Id ||
            recipientType.Id == RecipientType.Driver.Id)
        {
            return TimeSpan.FromHours(12);
        }

        return TimeSpan.FromDays(7);
    }

    /// <summary>
    /// How long the row survives after it expired, before being deleted for good.
    /// </summary>
    public static TimeSpan PurgeAfterExpiry(RecipientType recipientType)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        if (recipientType.Id == RecipientType.DesktopUser.Id ||
            recipientType.Id == RecipientType.Driver.Id)
        {
            return TimeSpan.FromDays(7);
        }

        return TimeSpan.FromDays(30);
    }

    /// <summary>
    /// Longest retention of them all. Anything a patient or an integration could read,
    /// and the fallback for rows that carry no audience.
    /// </summary>
    public static TimeSpan LongestPurgeWindow()
    {
        return TimeSpan.FromDays(30);
    }

    /// <summary>
    /// Audiences that only ever involve staff, purged on the shorter window.
    /// </summary>
    /// <remarks>
    /// They are the bulk of what the system writes: six of the eight wired events produce
    /// an office notice. Keeping them as long as a patient's record would multiply by four
    /// the size of the largest category, for notices whose whole purpose expired with the
    /// shift they belonged to.
    /// </remarks>
    public static IReadOnlyList<RecipientType> ShortLivedAudiences =>
    [
        RecipientType.DesktopUser,
        RecipientType.Driver
    ];

    /// <summary>
    /// Purge window that applies when every audience of a notification is short lived.
    /// </summary>
    public static TimeSpan ShortestPurgeWindow()
    {
        return PurgeAfterExpiry(RecipientType.DesktopUser);
    }

    /// <summary>
    /// How long a signal is worth acting on.
    /// </summary>
    /// <remarks>
    /// A signal says "the route on your screen is stale". The application deletes it the
    /// moment it acts on it, so this only governs the ones nobody consumes — a phone that
    /// was off, an app that was uninstalled mid-shift.
    ///
    /// <para>
    /// One hour rather than the twelve a driver notice gets: past that the app has reloaded
    /// its schedule for its own reasons and the signal would only make it reload again for
    /// a change it already has.
    /// </para>
    /// </remarks>
    public static TimeSpan SignalVisibleFor()
    {
        return TimeSpan.FromHours(1);
    }

    /// <summary>
    /// Expiry of a notification addressed to several audiences: the most generous one
    /// wins, so nobody loses a notice early because it was shared.
    /// </summary>
    /// <param name="isSignal">
    /// True for a notification no person will read. Signals age out in
    /// <see cref="SignalVisibleFor"/> instead of the audience window.
    /// </param>
    public static DateTime ResolveExpiry(
        DateTime createdAtUtc,
        IEnumerable<RecipientType> audiences,
        bool isSignal = false)
    {
        ArgumentNullException.ThrowIfNull(audiences);

        if (isSignal)
            return createdAtUtc.Add(SignalVisibleFor());

        var longest = audiences
            .Select(VisibleFor)
            .DefaultIfEmpty(TimeSpan.FromDays(7))
            .Max();

        return createdAtUtc.Add(longest);
    }
}
