using Raphael.Notification.Application.Interfaces.Engine;
using Raphael.Notification.Application.Interfaces.Events;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationService
{
    private readonly INotificationEngine _notificationEngine;

    public NotificationService(
        INotificationEngine notificationEngine)
    {
        _notificationEngine = notificationEngine;
    }

    public async Task PublishAsync(
        string eventCode,
        Guid aggregateId,
        Dictionary<string, object?> data,
        Guid? performedByUserId = null,
        CancellationToken cancellationToken = default)
    {
        var context = new BusinessEventContext
        {
            EventCode = eventCode,
            AggregateId = aggregateId,
            PerformedByUserId = performedByUserId
        };

        foreach (var item in data)
        {
            context.Data[item.Key] = item.Value;
        }

        await _notificationEngine.ProcessAsync(
            context,
            cancellationToken);
    }
}