using Raphael.Notification.Application.Interfaces.Persistence;

namespace Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;

public class MarkNotificationAcknowledgedHandler
{
    private readonly INotificationRecipientRepository _repository;


    public MarkNotificationAcknowledgedHandler(
        INotificationRecipientRepository repository)
    {
        _repository = repository;
    }


    public async Task Handle(
        MarkNotificationAcknowledgedCommand command,
        CancellationToken cancellationToken = default)
    {
        var recipient =
            await _repository.GetByIdAsync(
                command.NotificationRecipientId,
                cancellationToken);


        if (recipient == null)
        {
            throw new KeyNotFoundException(
                "Notification recipient not found.");
        }


        recipient.Acknowledge();


        await _repository.UpdateAsync(
            recipient,
            cancellationToken);
    }
}