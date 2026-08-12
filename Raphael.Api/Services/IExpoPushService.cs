using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IExpoPushService
    {
        Task<ExpoPushResult> SendPushNotificationWithDetailsAsync(string expoToken, string title, string body, object? data = null);
        Task<bool> SendPushNotificationAsync(string expoToken, string title, string body, object? data = null);
    }
}
