using Raphael.Shared.Domain.Common;

namespace Raphael.Notification.Domain.Definitions;

public abstract class NotificationEnumeration : Enumeration
{
    public string DisplayName { get; }

    public string Description { get; }

    public int SortOrder { get; }

    protected NotificationEnumeration(
        int id,
        string code,
        string name,
        string displayName,
        string description,
        int sortOrder)
        : base(id, code, name)
    {
        DisplayName = displayName;
        Description = description;
        SortOrder = sortOrder;
    }
}

/*IsEnabled
Category
DefaultLifetime
CanBeUserConfigured*/