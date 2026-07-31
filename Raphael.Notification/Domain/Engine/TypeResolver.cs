using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Domain.Engine;

public class TypeResolver
{
    public NotificationType Resolve(
        NotificationType configuredType)
    {
        ArgumentNullException.ThrowIfNull(configuredType);

        return configuredType;
    }
}