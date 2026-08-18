using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Delivery;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;

using NotificationModel =
    Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Infrastructure.Delivery;

public sealed class PushSender : INotificationSender
{
    private readonly IPushTokenProvider _pushTokenProvider;
    private readonly IExpoPushService _expoPushService;

    public DeliveryChannel Channel
        => DeliveryChannel.Push;

    public PushSender(
        IPushTokenProvider pushTokenProvider,
        IExpoPushService expoPushService)
    {
        _pushTokenProvider = pushTokenProvider;
        _expoPushService = expoPushService;
    }

    public async Task<NotificationSenderResult> SendAsync(
        NotificationModel notification,
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        ArgumentNullException.ThrowIfNull(recipient);

        try
        {
            //
            // NotificationRecipient stores the Rider identifier
            // as Guid. The existing Raphael identifier converter
            // translates it back to the integer CustomerId.
            //

            var customerId =
                UserIdentifierConverter.ToInt(
                    recipient.RecipientId);

            if (customerId <= 0)
            {
                return NotificationSenderResult.Fail(
                    "Invalid customer identifier.");
            }

            //
            // Get Push Token
            //

            var pushToken =
                await _pushTokenProvider.GetPushTokenAsync(
                    customerId,
                    cancellationToken);

            if (string.IsNullOrWhiteSpace(pushToken))
            {
                return NotificationSenderResult.Fail(
                    "Push token not found for recipient.");
            }

            //
            // Push data
            //

            var data = new
            {
                notificationId = notification.Id,
                businessEventCode =
                    notification.BusinessEventCode
            };

            //
            // Send Push Notification
            //

            var result =
                await _expoPushService.SendAsync(
                    pushToken,
                    notification.Title,
                    notification.Message,
                    data,
                    cancellationToken);

            if (!result.Success)
            {
                return NotificationSenderResult.Fail(
                    result.ErrorMessage ??
                    "Push notification failed.");
            }

            return NotificationSenderResult.Ok(
                "Push notification sent successfully.");
        }
        catch (Exception ex)
        {
            return NotificationSenderResult.Fail(
                ex.Message);
        }
    }
}