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
    /// Expiry of a notification addressed to several audiences: the most generous one
    /// wins, so nobody loses a notice early because it was shared.
    /// </summary>
    public static DateTime ResolveExpiry(
        DateTime createdAtUtc,
        IEnumerable<RecipientType> audiences)
    {
        ArgumentNullException.ThrowIfNull(audiences);

        var longest = audiences
            .Select(VisibleFor)
            .DefaultIfEmpty(TimeSpan.FromDays(7))
            .Max();

        return createdAtUtc.Add(longest);
    }
}
