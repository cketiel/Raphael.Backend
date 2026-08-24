using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using Raphael.Notification.Infrastructure.Realtime.Models;
using Raphael.Notification.Infrastructure.Realtime.Services;
using Raphael.Shared.Definitions.Notifications;
using System.Security.Claims;

namespace Raphael.Notification.Infrastructure.Realtime.Hubs;

[Authorize]
public class NotificationHub : Hub<INotificationClient>
{
    private readonly IConnectionManager _connectionManager;
    private readonly NotificationRealtimeOptions _options;

    public NotificationHub(
        IConnectionManager connectionManager,
        IOptions<NotificationRealtimeOptions> options)
    {
        _connectionManager = connectionManager;
        _options = options.Value;
    }

    public override async Task OnConnectedAsync()
    {
        //
        // 1. Rider (Raphael.Rider). Its token carries CustomerId.
        //
        if (TryGetIntClaim("CustomerId", out var customerId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                NotificationGroups.Customer(customerId));
        }

        //
        // 2. Integration. Its short lived token, obtained with the API Key,
        //    carries IntegratorId.
        //
        if (TryGetIntClaim("IntegratorId", out var integratorId))
        {
            await Groups.AddToGroupAsync(
                Context.ConnectionId,
                NotificationGroups.Integrator(integratorId));
        }

        //
        // 3. Internal users: Raphael.Desktop and Raphael.Driver.
        //    Both live in the Users table, so the role decides which one is connecting.
        //
        if (TryGetIntClaim("UserId", out var userId) ||
            TryGetIntClaim(ClaimTypes.NameIdentifier, out userId))
        {
            var recipientType = ResolveInternalRecipientType();

            await _connectionManager.RegisterConnectionAsync(
                new UserConnection
                {
                    UserId = UserIdentifierConverter.ToGuid(userId, recipientType),
                    ConnectionId = Context.ConnectionId,
                    ConnectedAtUtc = DateTime.UtcNow
                });

            // Only the dispatch office listens to the shared broadcast.
            if (recipientType.Id == RecipientType.DesktopUser.Id)
            {
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    NotificationGroups.DesktopAudience);
            }
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // The IConnectionManager only cleans up if the record for the ConnectionId exists.
        await _connectionManager.RemoveConnectionAsync(Context.ConnectionId);

        // SignalR groups are automatically cleaned up upon disconnection;
        // there is no need to manually remove the member from the group.
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Decides whether an internal user is connecting from Raphael.Driver or from
    /// Raphael.Desktop, based on the role carried by the token.
    /// </summary>
    /// <remarks>
    /// Defaults to Driver, the least privileged of the two: while the deployment has not
    /// declared its driver roles, nobody is placed in the dispatch office broadcast.
    /// </remarks>
    private RecipientType ResolveInternalRecipientType()
    {
        if (_options.DriverRoleIds.Length == 0)
            return RecipientType.Driver;

        var isDriver = Context.User?
            .FindAll(ClaimTypes.Role)
            .Concat(Context.User.FindAll("Role"))
            .Any(claim =>
                int.TryParse(claim.Value, out var roleId) &&
                _options.DriverRoleIds.Contains(roleId))
            ?? false;

        return isDriver
            ? RecipientType.Driver
            : RecipientType.DesktopUser;
    }

    private bool TryGetIntClaim(string type, out int value)
    {
        var raw = Context.User?.FindFirst(type)?.Value;

        return int.TryParse(raw, out value) && value > 0;
    }
}
