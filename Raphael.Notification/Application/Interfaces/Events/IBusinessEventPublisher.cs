namespace Raphael.Notification.Application.Interfaces.Events;

public interface IBusinessEventPublisher
{
    Task PublishAsync(
        BusinessEventContext context,
        CancellationToken cancellationToken = default);
}