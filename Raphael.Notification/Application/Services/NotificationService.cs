using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Interfaces.Persistence;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Services;

public class NotificationService
{
    private readonly INotificationRepository _notificationRepository;


    public NotificationService(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }


    public async Task<Guid> CreateAsync(
        NotificationModel notification,
        CancellationToken cancellationToken = default)
    {
        await _notificationRepository.AddAsync(
            notification,
            cancellationToken);


        return notification.Id;
    }
}