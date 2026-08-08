using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;

namespace Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;

public sealed class MarkNotificationAcknowledgedHandler
{
    private readonly INotificationRecipientRepository _recipientRepository;
    private readonly INotificationDispatcher _dispatcher;

    public MarkNotificationAcknowledgedHandler(
        INotificationRecipientRepository recipientRepository,
        INotificationDispatcher dispatcher)
    {
        _recipientRepository = recipientRepository;
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        MarkNotificationAcknowledgedCommand command,
        CancellationToken cancellationToken = default)
    {
        var recipient =
            await _recipientRepository.GetByIdAsync(
                command.NotificationRecipientId,
                cancellationToken);

        if (recipient == null)
            return;

        recipient.MarkAcknowledged();

        await _recipientRepository.SaveChangesAsync(
            cancellationToken);

        await _dispatcher.NotifyAcknowledgedAsync(
            recipient.RecipientId,
            recipient.Id,
            cancellationToken);

        await _dispatcher.RefreshNotificationsAsync(
            recipient.RecipientId,
            cancellationToken);
    }
}