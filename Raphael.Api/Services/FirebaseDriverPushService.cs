using Raphael.Notification.Application.Interfaces.Delivery;

namespace Raphael.Api.Services
{
    /// <summary>
    /// Bridges the notification module to the Firebase sender that already lives here.
    /// </summary>
    /// <remarks>
    /// Raphael.Notification cannot reference Raphael.Api, and initialising the Firebase
    /// SDK a second time from that project would fail, so the module declares the
    /// interface and the API supplies the implementation.
    /// </remarks>
    public class FirebaseDriverPushService : IDriverPushService
    {
        private readonly IFirebaseMessagingService _firebaseMessagingService;

        public FirebaseDriverPushService(
            IFirebaseMessagingService firebaseMessagingService)
        {
            _firebaseMessagingService = firebaseMessagingService;
        }

        public Task<bool> SendAsync(
            string deviceToken,
            string title,
            string body,
            IDictionary<string, string>? data = null,
            CancellationToken cancellationToken = default)
        {
            return _firebaseMessagingService.SendNotificationToDriverAsync(
                deviceToken,
                title,
                body,
                data is null ? null : new Dictionary<string, string>(data));
        }
    }
}
