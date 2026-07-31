namespace Raphael.Notification.Domain.Definitions;

public sealed class NotificationType : NotificationEnumeration
{
    public static readonly NotificationType Notice =
        new(
            1,
            "NOTICE",
            "Notice",
            "General informational notification.",
            1,
            false,
            false);

    public static readonly NotificationType Alert =
        new(
            2,
            "ALERT",
            "Alert",
            "Notification requiring user attention.",
            2,
            true,
            false);

    public static readonly NotificationType Reminder =
        new(
            3,
            "REMINDER",
            "Reminder",
            "Reminder for a scheduled or upcoming activity.",
            3,
            false,
            true);

    public static readonly NotificationType Confirmation =
        new(
            4,
            "CONFIRMATION",
            "Confirmation",
            "Confirms that an action completed successfully.",
            4,
            false,
            false);

    public static readonly NotificationType Warning =
        new(
            5,
            "WARNING",
            "Warning",
            "Warns about a potential issue.",
            5,
            true,
            false);

    public static readonly NotificationType ActionRequired =
        new(
            6,
            "ACTION_REQUIRED",
            "Action Required",
            "Requires the user to perform an action.",
            6,
            true,
            true);

    /// <summary>
    /// Indicates whether this type normally requires user attention.
    /// </summary>
    public bool RequiresAttention { get; }

    /// <summary>
    /// Indicates whether this type normally expects user interaction.
    /// </summary>
    public bool RequiresUserAction { get; }

    private NotificationType(
        int id,
        string code,
        string name,
        string description,
        int sortOrder,
        bool requiresAttention,
        bool requiresUserAction)
        : base(
            id,
            code,
            name,
            description,
            sortOrder)
    {
        RequiresAttention = requiresAttention;
        RequiresUserAction = requiresUserAction;
    }
}