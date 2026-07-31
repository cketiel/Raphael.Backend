using Raphael.Notification.Application.Interfaces.Events;

namespace Raphael.Notification.Application.Interfaces.Engine;

public interface INotificationEngine
{
    Task ProcessAsync(
        BusinessEventContext context,
        CancellationToken cancellationToken = default);
}