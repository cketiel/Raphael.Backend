using Raphael.Shared.DTOs;

namespace Raphael.Notification.Application.Interfaces.Delivery;

public interface IExpoPushService
{
    Task<ExpoPushResult> SendAsync(
        string expoToken,
        string title,
        string body,
        object? data = null,
        CancellationToken cancellationToken = default);
}