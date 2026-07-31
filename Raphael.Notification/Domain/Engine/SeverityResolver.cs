using Raphael.Notification.Domain.Definitions;

namespace Raphael.Notification.Domain.Engine;

public class SeverityResolver
{
    public NotificationSeverity Resolve(
        NotificationSeverity configuredSeverity)
    {
        ArgumentNullException.ThrowIfNull(configuredSeverity);

        return configuredSeverity;
    }
}