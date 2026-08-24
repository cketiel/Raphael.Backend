using Microsoft.AspNetCore.SignalR;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using Raphael.Notification.Infrastructure.Realtime.Hubs;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Realtime.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly IConnectionManager _connectionManager;
    private readonly INotificationDeliveryRepository _deliveryRepository;

    public NotificationDispatcher(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        IConnectionManager connectionManager,
        INotificationDeliveryRepository deliveryRepository)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _deliveryRepository = deliveryRepository;
    }

    public async Task SendNotificationAsync(
        Guid recipientId,
        RecipientType recipientType,
        NotificationDto notification,
        CancellationToken cancellationToken = default)
    {
        var delivered = await SendAsync(
            recipientId,
            recipientType,
            client => client.ReceiveNotification(notification));

        var delivery = new NotificationDelivery(
            notification.Id,
            DeliveryChannel.InApp);

        if (delivered)
            delivery.MarkDelivered();
        else
            delivery.MarkFailed("Recipient offline");

        await _deliveryRepository.AddAsync(
            delivery,
            cancellationToken);

        await _deliveryRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task RefreshNotificationsAsync(
        Guid recipientId,
        RecipientType recipientType,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            recipientId,
            recipientType,
            client => client.RefreshNotifications());
    }

    public async Task NotifyViewedAsync(
        Guid recipientId,
        RecipientType recipientType,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            recipientId,
            recipientType,
            client => client.NotificationViewed(notificationRecipientId));
    }

    public async Task NotifyAcknowledgedAsync(
        Guid recipientId,
        RecipientType recipientType,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default)
    {
        await SendAsync(
            recipientId,
            recipientType,
            client => client.NotificationAcknowledged(notificationRecipientId));
    }

    /// <summary>
    /// Routes one message to its destination and reports whether anything was actually sent.
    /// </summary>
    /// <remarks>
    /// Riders and integrations are reached through their own SignalR group; the dispatch
    /// office through the shared broadcast group; a concrete internal user through the
    /// connections registered for it. Sending to every destination at once, as this class
    /// used to, put office notices on patients' phones.
    /// </remarks>
    private async Task<bool> SendAsync(
        Guid recipientId,
        RecipientType recipientType,
        Func<INotificationClient, Task> send)
    {
        ArgumentNullException.ThrowIfNull(recipientType);

        //
        // Rider (Raphael.Rider)
        //
        if (recipientType.Id == RecipientType.Rider.Id)
        {
            var customerId = UserIdentifierConverter.ToInt(recipientId);

            if (customerId <= 0)
                return false;

            await send(
                _hubContext.Clients.Group(
                    NotificationGroups.Customer(customerId)));

            return true;
        }

        //
        // External integration
        //
        if (recipientType.Id == RecipientType.Integration.Id)
        {
            var integratorId = UserIdentifierConverter.ToInt(recipientId);

            if (integratorId <= 0)
                return false;

            await send(
                _hubContext.Clients.Group(
                    NotificationGroups.Integrator(integratorId)));

            return true;
        }

        //
        // Whole dispatch office: one stored notification, one broadcast
        //
        if (UserIdentifierConverter.IsDesktopAudience(recipientId))
        {
            await send(
                _hubContext.Clients.Group(
                    NotificationGroups.DesktopAudience));

            return true;
        }

        //
        // One concrete internal user: a dispatcher or a driver
        //
        var connections =
            await _connectionManager.GetUserConnectionsAsync(recipientId);

        if (connections.Count == 0)
            return false;

        await Task.WhenAll(
            connections.Select(connection =>
                send(_hubContext.Clients.Client(connection.ConnectionId))));

        return true;
    }
}
