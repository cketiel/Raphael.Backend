using Raphael.Notification.Domain.Definitions;

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