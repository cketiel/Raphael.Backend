using Microsoft.AspNetCore.SignalR;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using Raphael.Notification.Infrastructure.Realtime.Hubs;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Realtime.Services;

public class NotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;
    private readonly IConnectionManager _connectionManager;

    public NotificationDispatcher(
        IHubContext<NotificationHub, INotificationClient> hubContext,
        IConnectionManager connectionManager)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
    }

    public async Task SendNotificationAsync(
        Guid userId,
        NotificationDto notification,
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
                    .ReceiveNotification(notification));

        await Task.WhenAll(tasks);
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