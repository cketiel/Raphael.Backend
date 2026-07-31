using Raphael.Notification.Infrastructure.Realtime.Models;

namespace Raphael.Notification.Infrastructure.Realtime.Stores;

public interface IConnectionStore
{
    Task AddConnectionAsync(UserConnection connection);

    Task RemoveConnectionAsync(string connectionId);

    Task<IReadOnlyCollection<UserConnection>> GetConnectionsAsync(Guid userId);

    Task<UserConnection?> GetConnectionAsync(string connectionId);
}