using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Catalog;

public sealed class NotificationRecipientDefinition
{
    public required RecipientType RecipientType { get; init; }
}