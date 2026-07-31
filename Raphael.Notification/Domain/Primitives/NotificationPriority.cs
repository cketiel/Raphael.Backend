using Raphael.Shared.Domain.Common;

namespace Raphael.Notification.Domain.Primitives;

public sealed class NotificationPriority : Enumeration
{
    public static readonly NotificationPriority Low =
        new(
            1,
            "LOW",
            "Low",
            "Low",
            "Low priority notification.",
            1,
            false,
            TimeSpan.FromDays(30));

    public static readonly NotificationPriority Medium =
        new(
            2,
            "MED",
            "Medium",
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
            "High",
            "High priority notification.",
            3,
            true,
            TimeSpan.FromDays(90));

    public static readonly NotificationPriority Critical =
        new(
            4,
            "CRT",
            "Critical",
            "Critical",
            "Critical priority notification.",
            4,
            true,
            TimeSpan.FromDays(180));

    public string DisplayName { get; }

    public string Description { get; }

    public int SortOrder { get; }

    public bool RequiresAcknowledgement { get; }

    public TimeSpan DefaultExpiration { get; }

    public bool IsHighPriority =>
        Id >= High.Id;

    private NotificationPriority(
        int id,
        string code,
        string name,
        string displayName,
        string description,
        int sortOrder,
        bool requiresAcknowledgement,
        TimeSpan defaultExpiration)
        : base(id, code, name)
    {
        DisplayName = displayName;
        Description = description;
        SortOrder = sortOrder;
        RequiresAcknowledgement = requiresAcknowledgement;
        DefaultExpiration = defaultExpiration;
    }
}