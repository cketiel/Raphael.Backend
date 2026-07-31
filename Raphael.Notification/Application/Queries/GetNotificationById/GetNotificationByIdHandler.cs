using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Interfaces.Persistence;

namespace Raphael.Notification.Application.Queries.GetNotificationById;

public class GetNotificationByIdHandler
{
    private readonly INotificationRepository _notificationRepository;


    public GetNotificationByIdHandler(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }


    public async Task<NotificationDto?> Handle(
        GetNotificationByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var notification =
            await _notificationRepository.GetByIdAsync(
                query.NotificationId,
                cancellationToken);


        if (notification == null)
        {
            return null;
        }


        return new NotificationDto
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

            Recipients = notification.Recipients
                .Select(r => new NotificationRecipientDto
                {
                    Id = r.Id,

                    RecipientId = r.RecipientId,

                    RecipientType = r.RecipientType.Name,

                    Status = r.Status.Name,

                    DeliveredAtUtc = r.DeliveredAtUtc,

                    ViewedAtUtc = r.ViewedAtUtc,

                    AcknowledgedAtUtc = r.AcknowledgedAtUtc

                })
                .ToList(),

            Actions = notification.Actions
                .Select(a => new NotificationActionDto
                {
                    Id = a.Id,

                    ActionCode = a.ActionCode,

                    SortOrder = a.SortOrder,

                    IsPrimary = a.IsPrimary

                })
                .ToList()
        };
    }
}