using Raphael.Shared.Definitions.Notifications;

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