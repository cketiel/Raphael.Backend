namespace Raphael.Notification.Catalog;

public sealed class NotificationActionDefinition
{
    public required string ActionCode { get; init; }

    public int Order { get; init; }

    public bool IsPrimary { get; init; }
}