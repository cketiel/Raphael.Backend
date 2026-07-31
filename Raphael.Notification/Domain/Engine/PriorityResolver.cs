using Raphael.Notification.Domain.Definitions;

namespace Raphael.Notification.Domain.Engine;

public class PriorityResolver
{
    public NotificationPriority Resolve(
        NotificationPriority configuredPriority)
    {
        ArgumentNullException.ThrowIfNull(configuredPriority);

        return configuredPriority;
    }
}