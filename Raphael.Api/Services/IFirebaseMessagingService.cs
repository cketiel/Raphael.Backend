namespace Raphael.Api.Services
{
    public interface IFirebaseMessagingService
    {
        Task<bool> SendNotificationToDriverAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null);
    }
}
