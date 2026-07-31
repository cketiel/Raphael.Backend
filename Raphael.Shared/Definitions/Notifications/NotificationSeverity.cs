namespace Raphael.Shared.Definitions.Notifications;

public sealed class NotificationSeverity : NotificationEnumeration
{
    public static readonly NotificationSeverity Information =
        new(
            1,
            "INFORMATION",
            "Information",
            "Informational notification.",
            1,
            false,
            false);

    public static readonly NotificationSeverity Success =
        new(
            2,
            "SUCCESS",
            "Success",
            "Successful operation notification.",
            2,
            false,
            false);

    public static readonly NotificationSeverity Warning =
        new(
            3,
            "WARNING",
            "Warning",
            "Warning notification requiring user attention.",
            3,
            true,
            false);

    public static readonly NotificationSeverity Error =
        new(
            4,
            "ERROR",
            "Error",
            "Error notification.",
            4,
            true,
            false);

    public static readonly NotificationSeverity Critical =
        new(
            5,
            "CRITICAL",
            "Critical",
            "Critical notification requiring immediate action.",
            5,
            true,
            true);

    /// <summary>
    /// Indicates whether the notification
    /// should draw the user's attention.
    /// </summary>
    public bool RequiresAttention { get; }

    /// <summary>
    /// Indicates whether the notification
    /// requires immediate action.
    /// </summary>
    public bool RequiresImmediateAction { get; }

    private NotificationSeverity(
        int id,
        string code,
        string name,
        string description,
        int sortOrder,
        bool requiresAttention,
        bool requiresImmediateAction)
        : base(
            id,
            code,
            name,
            description,
            sortOrder)
    {
        RequiresAttention = requiresAttention;
        RequiresImmediateAction = requiresImmediateAction;
    }
}