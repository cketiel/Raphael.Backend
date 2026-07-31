using Raphael.Notification.Infrastructure.Realtime.Models;

namespace Raphael.Notification.Infrastructure.Realtime.Services;

public interface IConnectionManager
{
    Task RegisterConnectionAsync(UserConnection connection);

    Task RemoveConnectionAsync(string connectionId);

    Task<IReadOnlyCollection<UserConnection>> GetUserConnectionsAsync(Guid userId);

    Task<UserConnection?> GetConnectionAsync(string connectionId);

    Task<bool> IsUserOnlineAsync(Guid userId);
}