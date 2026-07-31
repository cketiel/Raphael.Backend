using Raphael.Notification.Application.Interfaces.Persistence;

namespace Raphael.Notification.Application.Commands.MarkNotificationViewed;

public class MarkNotificationViewedHandler
{
    private readonly INotificationRecipientRepository _repository;


    public MarkNotificationViewedHandler(
        INotificationRecipientRepository repository)
    {
        _repository = repository;
    }


    public async Task Handle(
        MarkNotificationViewedCommand command,
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


        recipient.MarkViewed();


        await _repository.UpdateAsync(
            recipient,
            cancellationToken);
    }
}