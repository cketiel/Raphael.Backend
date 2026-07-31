namespace Raphael.Notification.Infrastructure.Realtime.Models;

public class UserConnection
{
    public Guid UserId { get; init; }

    public string ConnectionId { get; init; } = string.Empty;

    public DateTime ConnectedAtUtc { get; init; }

    public string? Device { get; init; }

    public string? Platform { get; init; }
}