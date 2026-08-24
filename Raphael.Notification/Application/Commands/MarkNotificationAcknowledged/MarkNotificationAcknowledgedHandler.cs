using Microsoft.Extensions.Logging;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Application.Services;
using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;

public sealed class MarkNotificationAcknowledgedHandler
{
    private readonly INotificationRecipientRepository _recipientRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly NotificationService _notificationService;
    private readonly ILogger<MarkNotificationAcknowledgedHandler> _logger;

    public MarkNotificationAcknowledgedHandler(
        INotificationRecipientRepository recipientRepository,
        INotificationRepository notificationRepository,
        INotificationDispatcher dispatcher,
        NotificationService notificationService,
        ILogger<MarkNotificationAcknowledgedHandler> logger)
    {
        _recipientRepository = recipientRepository;
        _notificationRepository = notificationRepository;
        _dispatcher = dispatcher;
        _notificationService = notificationService;
        _logger = logger;
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
            recipient.RecipientType,
            recipient.Id,
            cancellationToken);

        await _dispatcher.RefreshNotificationsAsync(
            recipient.RecipientId,
            recipient.RecipientType,
            cancellationToken);

        await ChainWillCallAcknowledgementAsync(
            recipient.NotificationId,
            recipient.RecipientTypeId,
            cancellationToken);
    }

    /// <summary>
    /// When a dispatcher takes charge of a Will Call, the patient is told the office
    /// knows and by when a vehicle should reach them.
    /// </summary>
    /// <remarks>
    /// Acknowledge is the trigger, not Viewed: glancing at a list is not the same as
    /// taking responsibility, and this message promises the patient that somebody did.
    ///
    /// <para>
    /// The deadline is recomputed from the activation instant carried in the original
    /// notification, never from now. The hour belongs to the patient, and it started
    /// running the moment they said they were ready, however long the notice sat unread.
    /// </para>
    /// </remarks>
    private async Task ChainWillCallAcknowledgementAsync(
        Guid notificationId,
        int recipientTypeId,
        CancellationToken cancellationToken)
    {
        // Only the dispatch office can take charge. A patient acknowledging their own
        // copy is not the office answering them.
        if (recipientTypeId != RecipientType.DesktopUser.Id)
            return;

        try
        {
            var notification =
                await _notificationRepository.GetByIdAsync(
                    notificationId,
                    cancellationToken);

            if (notification is null ||
                notification.BusinessEventCode != BusinessEventCodes.WillCallActivated)
            {
                return;
            }

            var metadata = notification.Metadata
                .ToDictionary(x => x.Key, x => x.Value);

            if (!metadata.TryGetValue(NotificationMetadataKeys.RiderId, out var riderId) ||
                !int.TryParse(riderId, out var riderIdValue) ||
                riderIdValue <= 0)
            {
                return;
            }

            var data = new Dictionary<string, object?>
            {
                [BusinessEventDataKeys.RiderId] = riderIdValue
            };

            if (metadata.TryGetValue(NotificationMetadataKeys.TripId, out var tripId))
                data[BusinessEventDataKeys.TripId] = tripId;

            if (metadata.TryGetValue(
                    NotificationMetadataKeys.WillCallActivatedAtUtc,
                    out var activatedAt))
            {
                data[BusinessEventDataKeys.WillCallActivatedAtUtc] = activatedAt;
            }

            await _notificationService.PublishAsync(
                eventCode: BusinessEventCodes.WillCallAcknowledged,
                aggregateId: notification.Id,
                data: data,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            // Acknowledging succeeded. Failing to tell the patient must not undo that,
            // nor report an error to the dispatcher who did take charge.
            _logger.LogError(
                ex,
                "Failed to chain {EventCode} from notification {NotificationId}.",
                BusinessEventCodes.WillCallAcknowledged,
                notificationId);
        }
    }
}
