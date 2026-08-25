using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Helpers;
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

                // Only the row that belongs to whoever asked. The other audiences of the
                // same notification are none of this caller's business: sending them
                // would hand a dispatcher the recipient identifiers of a patient.
                Recipients = notification.Recipients
                .Where(r => r.RecipientId == query.RecipientId
                            && r.RecipientTypeId == query.RecipientType.Id)
                .Select(r => new NotificationRecipientDto
                {
                    Id = r.Id,
                    RecipientId = r.RecipientId,
                    RecipientType = r.RecipientType.Name,
                    IsBroadcast = UserIdentifierConverter.IsDesktopAudience(r.RecipientId),
                    Status = r.Status.Name,
                    ViewedAtUtc = r.ViewedAtUtc,
                    DeliveredAtUtc = r.DeliveredAtUtc,
                    AcknowledgedAtUtc = r.AcknowledgedAtUtc
                }).ToList(),

                Actions = notification.Actions
                .OrderBy(a => a.SortOrder)
                .Select(a => new NotificationActionDto
                {
                    Id = a.Id,
                    ActionCode = a.ActionCode,
                    SortOrder = a.SortOrder,
                    IsPrimary = a.IsPrimary
                }).ToList(),

                // What the client needs to render this in its own language and to open
                // the right screen. Identifiers only, never PHI.
                Metadata = notification.Metadata
                .ToDictionary(m => m.Key, m => m.Value)
            })
            .ToList();
    }
}