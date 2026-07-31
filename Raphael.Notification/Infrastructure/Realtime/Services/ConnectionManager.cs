using Raphael.Notification.Infrastructure.Realtime.Models;
using Raphael.Notification.Infrastructure.Realtime.Stores;

namespace Raphael.Notification.Infrastructure.Realtime.Services;

public class ConnectionManager : IConnectionManager
{
    private readonly IConnectionStore _connectionStore;

    public ConnectionManager(
        IConnectionStore connectionStore)
    {
        _connectionStore = connectionStore;
    }

    public async Task RegisterConnectionAsync(UserConnection connection)
    {
        await _connectionStore.AddConnectionAsync(connection);
    }

    public async Task RemoveConnectionAsync(string connectionId)
    {
        await _connectionStore.RemoveConnectionAsync(connectionId);
    }

    public async Task<IReadOnlyCollection<UserConnection>> GetUserConnectionsAsync(Guid userId)
    {
        return await _connectionStore.GetConnectionsAsync(userId);
    }

    public async Task<UserConnection?> GetConnectionAsync(string connectionId)
    {
        return await _connectionStore.GetConnectionAsync(connectionId);
    }

    public async Task<bool> IsUserOnlineAsync(Guid userId)
    {
        var connections =
            await _connectionStore.GetConnectionsAsync(userId);

        return connections.Count > 0;
    }
}