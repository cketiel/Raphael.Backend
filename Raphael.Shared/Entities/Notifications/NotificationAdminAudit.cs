namespace Raphael.Shared.Entities.Notifications;

/// <summary>
/// Who did something irreversible to the notification records, and when.
/// </summary>
/// <remarks>
/// Archiving keeps a record from ever being deleted; purging and deleting archived rows
/// destroy records outright. Both are decisions somebody makes on behalf of the business,
/// and a system that lets a record disappear without saying who removed it cannot answer
/// the one question that matters afterwards.
///
/// <para>
/// ⚠️ <b>There is deliberately no foreign key to the notification.</b> The whole point of
/// this table is to outlive what it describes: a cascade from a deleted notification would
/// erase the evidence of its deletion.
/// </para>
///
/// <para>
/// ⚠️ Nothing here carries patient data. <see cref="Details"/> holds counts and identifiers
/// only, and no caller should put anything else in it.
/// </para>
/// </remarks>
public class NotificationAdminAudit
{
    public Guid Id { get; private set; }

    /// <summary>One of <c>NotificationAdminActions</c>.</summary>
    public string Action { get; private set; }

    public int? PerformedByUserId { get; private set; }

    /// <summary>
    /// Copied in rather than joined.
    /// </summary>
    /// <remarks>
    /// A user can be renamed or removed; the audit has to keep saying who it was at the
    /// time. Joining to the users table would quietly rewrite history.
    /// </remarks>
    public string PerformedByUsername { get; private set; }

    public DateTime PerformedAtUtc { get; private set; }

    /// <summary>The notification acted on, when the action targets one.</summary>
    public Guid? NotificationId { get; private set; }

    /// <summary>How many rows the action reached. One, or the size of a bulk run.</summary>
    public int AffectedCount { get; private set; }

    /// <summary>Short, machine-written context. Never free text from a user.</summary>
    public string? Details { get; private set; }

    private NotificationAdminAudit()
    {
        // Required by EF Core
    }

    public NotificationAdminAudit(
        string action,
        int? performedByUserId,
        string? performedByUsername,
        Guid? notificationId = null,
        int affectedCount = 1,
        string? details = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        Id = Guid.NewGuid();
        Action = action;
        PerformedByUserId = performedByUserId;

        // An action with no name attached is worse than useless, so it is recorded as
        // unknown rather than left blank and mistaken for a missing column.
        PerformedByUsername = string.IsNullOrWhiteSpace(performedByUsername)
            ? "unknown"
            : performedByUsername;

        PerformedAtUtc = DateTime.UtcNow;
        NotificationId = notificationId;
        AffectedCount = affectedCount;
        Details = details;
    }
}
