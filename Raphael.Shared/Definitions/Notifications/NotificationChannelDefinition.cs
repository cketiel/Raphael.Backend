using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Catalog;

public sealed class NotificationChannelDefinition
{
    public required DeliveryChannel Channel { get; init; }
}