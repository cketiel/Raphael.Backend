using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Interfaces.Persistence;

namespace Raphael.Notification.Application.Queries.GetRecipientNotifications;

public class GetRecipientNotificationsHandler
{
    private readonly INotificationRepository _notificationRepository;


    public GetRecipientNotificationsHandler(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }


    public async Task<IReadOnlyList<NotificationDto>> Handle(
        GetRecipientNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        var notifications =
            await _notificationRepository.GetByRecipientAsync(
                query.RecipientId,
                query.RecipientType.Id,
                cancellationToken);


        return notifications
            .Select(notification => new NotificationDto
            {
                Id = notification.Id,

                BusinessEventCode = notification.BusinessEventCode,

                Priority = notification.Priority.Name,

                Severity = notification.Severity.Name,

                Type = notification.Type.Name,

                Status = notification.Status.Name,

                Title = notification.Title,

                Message = notification.Message,

                CreatedAtUtc = notification.CreatedAtUtc,

                ExpiresAtUtc = notification.ExpiresAtUtc,

                // Map the recipients 
                Recipients = notification.Recipients
                .Where(r => r.RecipientId == query.RecipientId
                            && r.RecipientTypeId == query.RecipientType.Id)
                .Select(r => new NotificationRecipientDto
                {
                    Id = r.Id, 
                    RecipientId = r.RecipientId,
                    Status = r.Status.Name,
                    ViewedAtUtc = r.ViewedAtUtc,
                    DeliveredAtUtc = r.DeliveredAtUtc,
                    AcknowledgedAtUtc = r.AcknowledgedAtUtc
                }).ToList()
            })
            .ToList();
    }
}