namespace Raphael.Notification.Application.Interfaces.Delivery;

/// <summary>
/// Resolves the device token to push to. Riders and drivers live in different tables
/// and use different providers, so each one has its own lookup.
/// </summary>
public interface IPushTokenProvider
{
    /// <summary>Expo token of a patient using Raphael.Rider.</summary>
    Task<string?> GetRiderPushTokenAsync(
        int customerId,
        CancellationToken cancellationToken = default);

    /// <summary>FCM/APNs token of a driver using Raphael.Driver.</summary>
    Task<string?> GetDriverPushTokenAsync(
        int userId,
        CancellationToken cancellationToken = default);
}
