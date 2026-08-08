using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;

namespace Raphael.Notification.Application.Commands.MarkNotificationViewed;

public sealed class MarkNotificationViewedHandler
{
    private readonly INotificationRecipientRepository _recipientRepository;
    private readonly INotificationDispatcher _dispatcher;

    public MarkNotificationViewedHandler(
        INotificationRecipientRepository recipientRepository,
        INotificationDispatcher dispatcher)
    {
        _recipientRepository = recipientRepository;
        _dispatcher = dispatcher;
    }

    public async Task Handle(
        MarkNotificationViewedCommand command,
        CancellationToken cancellationToken = default)
    {
        var recipient =
            await _recipientRepository.GetByIdAsync(
                command.NotificationRecipientId,
                cancellationToken);

        if (recipient == null)
            return;

        recipient.MarkViewed();

        await _recipientRepository.SaveChangesAsync(
            cancellationToken);

        await _dispatcher.NotifyViewedAsync(
            recipient.RecipientId,
            recipient.Id,
            cancellationToken);

        await _dispatcher.RefreshNotificationsAsync(
            recipient.RecipientId,
            cancellationToken);
    }
}