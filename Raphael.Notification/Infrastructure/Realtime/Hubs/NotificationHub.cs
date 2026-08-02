using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using Raphael.Notification.Infrastructure.Realtime.Models;
using Raphael.Notification.Infrastructure.Realtime.Services;
using Raphael.Notification.Infrastructure.Realtime.Stores;
using System.Security.Claims;

namespace Raphael.Notification.Infrastructure.Realtime.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;

    public NotificationHub(IConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public override async Task OnConnectedAsync()
    {
        // 1. Logic for Internal Users 
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirst("UserId");

        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            var connection = new UserConnection
            {
                UserId = userId,
                ConnectionId = Context.ConnectionId,
                ConnectedAtUtc = DateTime.UtcNow
            };

            await _connectionManager.RegisterConnectionAsync(connection);
        }

        // 2. Logic for the Rider App (Group Implementation)
        // We look for the CustomerId included in the Rider's JWT
        var customerIdClaim = Context.User?.FindFirst("CustomerId");

        if (customerIdClaim is not null)
        {
            // We add the connection to a unique group for this patient: "Customer_123"
            // This allows sending notifications without touching the GUID-based IConnectionManager.
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Customer_{customerIdClaim.Value}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // The IConnectionManager only cleans up if the record for the ConnectionId exists.
        await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);

        // SignalR groups are automatically cleaned up upon disconnection; 
        // there is no need to manually remove the Customer from the group.
        await base.OnDisconnectedAsync(exception);
    }
}