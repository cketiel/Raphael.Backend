using System.Collections.Concurrent;
using Raphael.Notification.Infrastructure.Realtime.Models;

namespace Raphael.Notification.Infrastructure.Realtime.Stores;

public class InMemoryConnectionStore : IConnectionStore
{
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<string, UserConnection>>
        _connections = new();

    public Task AddConnectionAsync(UserConnection connection)
    {
        var userConnections = _connections.GetOrAdd(
            connection.UserId,
            _ => new ConcurrentDictionary<string, UserConnection>());

        userConnections[connection.ConnectionId] = connection;

        return Task.CompletedTask;
    }

    public Task RemoveConnectionAsync(string connectionId)
    {
        foreach (var pair in _connections)
        {
            if (pair.Value.TryRemove(connectionId, out _))
            {
                if (pair.Value.IsEmpty)
                {
                    _connections.TryRemove(pair.Key, out _);
                }

                break;
            }
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<UserConnection>> GetConnectionsAsync(Guid userId)
    {
        if (_connections.TryGetValue(userId, out var userConnections))
        {
            return Task.FromResult<IReadOnlyCollection<UserConnection>>(
                userConnections.Values.ToList());
        }

        return Task.FromResult<IReadOnlyCollection<UserConnection>>(
            Array.Empty<UserConnection>());
    }

    public Task<UserConnection?> GetConnectionAsync(string connectionId)
    {
        foreach (var pair in _connections)
        {
            if (pair.Value.TryGetValue(connectionId, out var connection))
            {
                return Task.FromResult<UserConnection?>(connection);
            }
        }

        return Task.FromResult<UserConnection?>(null);
    }
}