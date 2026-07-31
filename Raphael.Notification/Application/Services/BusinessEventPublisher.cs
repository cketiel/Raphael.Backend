using Raphael.Notification.Application.Interfaces.Engine;
using Raphael.Notification.Application.Interfaces.Events;

namespace Raphael.Notification.Application.Services;

public sealed class BusinessEventPublisher : IBusinessEventPublisher
{
    private readonly INotificationEngine _notificationEngine;

    public BusinessEventPublisher(
        INotificationEngine notificationEngine)
    {
        _notificationEngine = notificationEngine;
    }

    public async Task PublishAsync(
        BusinessEventContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        await _notificationEngine.ProcessAsync(
            context,
            cancellationToken);
    }
}