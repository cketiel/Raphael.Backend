namespace Raphael.Shared.Definitions.Notifications;

public sealed class NotificationPriority : NotificationEnumeration
{
    public static readonly NotificationPriority Low =
        new(
            1,
            "LOW",
            "Low",
            "Low priority notification.",
            1,
            false,
            TimeSpan.FromDays(30));

    public static readonly NotificationPriority Medium =
        new(
            2,
            "MEDIUM",
            "Medium",
            "Medium priority notification.",
            2,
            false,
            TimeSpan.FromDays(30));

    public static readonly NotificationPriority High =
        new(
            3,
            "HIGH",
            "High",
            "High priority notification.",
            3,
            true,
            TimeSpan.FromDays(90));

    public static readonly NotificationPriority Critical =
        new(
            4,
            "CRITICAL",
            "Critical",
            "Critical notification requiring immediate attention.",
            4,
            true,
            TimeSpan.FromDays(180));

    /// <summary>
    /// Indicates whether notifications with this priority
    /// require explicit user acknowledgement.
    /// </summary>
    public bool RequiresAcknowledgement { get; }

    /// <summary>
    /// Default retention period for notifications
    /// with this priority.
    /// </summary>
    public TimeSpan DefaultExpiration { get; }

    private NotificationPriority(
        int id,
        string code,
        string name,
        string description,
        int sortOrder,
        bool requiresAcknowledgement,
        TimeSpan defaultExpiration)
        : base(
            id,
            code,
            name,
            description,
            sortOrder)
    {
        RequiresAcknowledgement = requiresAcknowledgement;
        DefaultExpiration = defaultExpiration;
    }
}