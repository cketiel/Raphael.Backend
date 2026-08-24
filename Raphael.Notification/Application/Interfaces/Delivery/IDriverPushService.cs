namespace Raphael.Notification.Application.Interfaces.Delivery;

/// <summary>
/// Native push towards Raphael.Driver (FCM/APNs).
/// </summary>
/// <remarks>
/// Declared here and implemented in Raphael.Api, which already owns the Firebase
/// credentials and initialises the SDK. Initialising it a second time from this project
/// would fail: FirebaseApp only admits one default instance per process.
/// </remarks>
public interface IDriverPushService
{
    Task<bool> SendAsync(
        string deviceToken,
        string title,
        string body,
        IDictionary<string, string>? data = null,
        CancellationToken cancellationToken = default);
}
