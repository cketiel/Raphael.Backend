namespace Raphael.Shared.Definitions.Notifications;

/// <summary>
/// The administrative actions worth recording who performed.
/// </summary>
/// <remarks>
/// All of them either keep a record from being deleted or delete it. When somebody asks
/// six months from now why a notification is not there, the answer has to be a name and a
/// timestamp, not a shrug.
/// </remarks>
public static class NotificationAdminActions
{
    /// <summary>A notification was marked to survive the cleanup.</summary>
    public const string Archive = "ARCHIVE";

    /// <summary>That decision was taken back; the notification ages normally again.</summary>
    public const string Unarchive = "UNARCHIVE";

    /// <summary>The cleanup was run out of turn.</summary>
    public const string RetentionRun = "RETENTION_RUN";

    /// <summary>One archived notification was deleted for good.</summary>
    public const string DeleteArchived = "DELETE_ARCHIVED";

    /// <summary>Every archived notification was deleted for good.</summary>
    public const string DeleteArchivedAll = "DELETE_ARCHIVED_ALL";
}
