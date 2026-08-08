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
        IConnectionManager connectionManager, INotificationDeliveryRepository deliveryRepository)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _deliveryRepository = deliveryRepository;
    }

    public async Task SendNotificationAsync(
    Guid userId,
    NotificationDto notification,
    CancellationToken cancellationToken = default)
    {
        bool delivered = false;

        //
        // Desktop users
        //

        var connections =
            await _connectionManager.GetUserConnectionsAsync(userId);

        if (connections.Any())
        {
            var tasks =
                connections.Select(c =>
                    _hubContext
                        .Clients
                        .Client(c.ConnectionId)
                        .ReceiveNotification(notification));

            await Task.WhenAll(tasks);

            delivered = true;
        }

        //
        // Rider
        //

        var customerId =
            UserIdentifierConverter.ToInt(userId);

        var groupName =
            $"Customer_{customerId}";

        await _hubContext
            .Clients
            .Group(groupName)
            .ReceiveNotification(notification);

        delivered = true;

        //
        // Delivery Record
        //

        var delivery =
            new NotificationDelivery(
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
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var connections =
            await _connectionManager.GetUserConnectionsAsync(userId);

        if (!connections.Any())
            return;

        var tasks = connections
            .Select(connection =>
                _hubContext
                    .Clients
                    .Client(connection.ConnectionId)
                    .RefreshNotifications());

        await Task.WhenAll(tasks);
    }

    public async Task NotifyViewedAsync(
        Guid userId,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default)
    {
        var connections =
            await _connectionManager.GetUserConnectionsAsync(userId);

        if (!connections.Any())
            return;

        var tasks = connections
            .Select(connection =>
                _hubContext
                    .Clients
                    .Client(connection.ConnectionId)
                    .NotificationViewed(notificationRecipientId));

        await Task.WhenAll(tasks);
    }

    public async Task NotifyAcknowledgedAsync(
        Guid userId,
        Guid notificationRecipientId,
        CancellationToken cancellationToken = default)
    {
        var connections =
            await _connectionManager.GetUserConnectionsAsync(userId);

        if (!connections.Any())
            return;

        var tasks = connections
            .Select(connection =>
                _hubContext
                    .Clients
                    .Client(connection.ConnectionId)
                    .NotificationAcknowledged(notificationRecipientId));

        await Task.WhenAll(tasks);
    }
}