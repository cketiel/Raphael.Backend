using Raphael.Notification.Application.Interfaces.Persistence;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Commands.CreateNotification;

public class CreateNotificationHandler
{
    private readonly INotificationRepository _notificationRepository;


    public CreateNotificationHandler(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }


    public async Task<Guid> Handle(
        CreateNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        var request = command.Request;


        var notification = new NotificationModel(
            request.BusinessEventCode,
            null!,
            null!,
            null!,
            request.Title,
            request.Message,
            request.ExpiresAtUtc);


        await _notificationRepository.AddAsync(
            notification,
            cancellationToken);


        return notification.Id;
    }
}