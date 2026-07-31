using Raphael.Shared.Domain.Common;

namespace Raphael.Shared.Definitions.Notifications;

public abstract class NotificationEnumeration : Enumeration
{
    /// <summary>
    /// Functional description of the notification concept.
    /// This value is intended for developers and documentation,
    /// not for end-user display.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Defines the default ordering of the enumeration.
    /// Useful for sorting in APIs, administration pages and configuration.
    /// </summary>
    public int SortOrder { get; }

    protected NotificationEnumeration(
        int id,
        string code,
        string name,
        string description,
        int sortOrder)
        : base(id, code, name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        ArgumentOutOfRangeException.ThrowIfNegative(sortOrder);

        Description = description;
        SortOrder = sortOrder;
    }
}

/*IsEnabled
Category
DefaultLifetime
CanBeUserConfigured*/