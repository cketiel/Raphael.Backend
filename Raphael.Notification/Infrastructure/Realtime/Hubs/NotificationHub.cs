using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using Raphael.Notification.Infrastructure.Realtime.Services;
using Raphael.Notification.Infrastructure.Realtime.Models;
using Raphael.Notification.Infrastructure.Realtime.Stores;
using System.Security.Claims;

namespace Raphael.Notification.Infrastructure.Realtime.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;

    public NotificationHub(
        IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);

        if (userIdClaim is not null &&
            Guid.TryParse(userIdClaim.Value, out var userId))
        {
            var connection = new UserConnection
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                ConnectedAtUtc = DateTime.UtcNow
            };

            await _connectionManager.RegisterConnectionAsync(connection);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);

        await base.OnDisconnectedAsync(exception);
    }
}