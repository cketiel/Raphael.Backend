namespace Raphael.Notification.Application.Interfaces.Delivery;

public interface IPushTokenProvider
{
    Task<string?> GetPushTokenAsync(
        int customerId,
        CancellationToken cancellationToken = default);
}