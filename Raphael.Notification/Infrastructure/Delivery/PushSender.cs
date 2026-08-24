using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Delivery;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;

using NotificationModel =
    Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Infrastructure.Delivery;

/// <summary>
/// Sends the Push channel. Riders go through Expo and drivers through Firebase,
/// so the recipient type decides the provider and the token lookup.
/// </summary>
public sealed class PushSender : INotificationSender
{
    private readonly IPushTokenProvider _pushTokenProvider;
    private readonly IExpoPushService _expoPushService;
    private readonly IDriverPushService _driverPushService;

    public DeliveryChannel Channel
        => DeliveryChannel.Push;

    public PushSender(
        IPushTokenProvider pushTokenProvider,
        IExpoPushService expoPushService,
        IDriverPushService driverPushService)
    {
        _pushTokenProvider = pushTokenProvider;
        _expoPushService = expoPushService;
        _driverPushService = driverPushService;
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
            // The recipient stores the Raphael identifier as a Guid; the converter
            // gives back the integer CustomerId or UserId.
            //
            var id = UserIdentifierConverter.ToInt(recipient.RecipientId);

            if (id <= 0)
            {
                return NotificationSenderResult.Fail(
                    "Invalid recipient identifier.");
            }

            var recipientTypeId = recipient.RecipientTypeId;

            if (recipientTypeId == RecipientType.Rider.Id)
            {
                return await SendToRiderAsync(
                    notification,
                    id,
                    cancellationToken);
            }

            if (recipientTypeId == RecipientType.Driver.Id)
            {
                return await SendToDriverAsync(
                    notification,
                    id,
                    cancellationToken);
            }

            //
            // Desktop users and integrations have no device to push to: they are
            // reached in-app and, in the case of integrations, by their own endpoint.
            //
            return NotificationSenderResult.Fail(
                $"Push is not supported for recipient type {recipient.RecipientType.Code}.");
        }
        catch (Exception ex)
        {
            return NotificationSenderResult.Fail(
                ex.Message);
        }
    }

    private async Task<NotificationSenderResult> SendToRiderAsync(
        NotificationModel notification,
        int customerId,
        CancellationToken cancellationToken)
    {
        var pushToken =
            await _pushTokenProvider.GetRiderPushTokenAsync(
                customerId,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return NotificationSenderResult.Fail(
                "Push token not found for recipient.");
        }

        var result =
            await _expoPushService.SendAsync(
                pushToken,
                notification.Title,
                notification.Message,
                BuildPayload(notification),
                cancellationToken);

        return result.Success
            ? NotificationSenderResult.Ok("Push notification sent successfully.")
            : NotificationSenderResult.Fail(
                result.ErrorMessage ?? "Push notification failed.");
    }

    private async Task<NotificationSenderResult> SendToDriverAsync(
        NotificationModel notification,
        int userId,
        CancellationToken cancellationToken)
    {
        var pushToken =
            await _pushTokenProvider.GetDriverPushTokenAsync(
                userId,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(pushToken))
        {
            return NotificationSenderResult.Fail(
                "Push token not found for recipient.");
        }

        var sent =
            await _driverPushService.SendAsync(
                pushToken,
                notification.Title,
                notification.Message,
                BuildPayload(notification),
                cancellationToken);

        return sent
            ? NotificationSenderResult.Ok("Push notification sent successfully.")
            : NotificationSenderResult.Fail("Push notification failed.");
    }

    /// <summary>
    /// Data travelling with the push. Carries identifiers only: the app loads the
    /// detail once the user opens it, already authenticated. Nothing here reaches
    /// a lock screen, but it does cross third party servers.
    /// </summary>
    private static Dictionary<string, string> BuildPayload(
        NotificationModel notification)
    {
        var payload = new Dictionary<string, string>
        {
            ["notificationId"] = notification.Id.ToString(),
            ["businessEventCode"] = notification.BusinessEventCode
        };

        var tripId = notification.Metadata
            .FirstOrDefault(x => x.Key == NotificationMetadataKeys.TripId);

        if (tripId is not null)
        {
            payload["tripId"] = tripId.Value;
        }

        return payload;
    }
}
