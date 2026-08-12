using Raphael.Shared.DTOs;
using System.Net.Http.Json;

namespace Raphael.Api.Services
{    
    public class ExpoPushService : IExpoPushService
    {
        private readonly HttpClient _httpClient;
        private const string ExpoApiUrl = "https://exp.host/--/api/v2/push/send";

        public ExpoPushService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ExpoPushResult> SendPushNotificationWithDetailsAsync(string expoToken, string title, string body, object? data = null)
        {
            // Token cleanup (remove quotes sometimes sent by the mobile device)
            string cleanToken = expoToken.Replace("\"", "").Trim();

            var messages = new[] {
                new {
                    to = cleanToken,
                    title = title,
                    body = body,
                    data = data,
                    sound = "default"
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(ExpoApiUrl, messages);
                var rawContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new ExpoPushResult
                    {
                        Success = false,
                        RawResponse = rawContent,
                        ErrorMessage = $"HTTP Error: {response.StatusCode}"
                    };
                }

                // If it is 200, we return the JSON containing the status "ok" or "error"
                return new ExpoPushResult
                {
                    Success = true,
                    RawResponse = rawContent
                };
            }
            catch (Exception ex)
            {
                return new ExpoPushResult { Success = false, ErrorMessage = ex.Message };
            }
        }
        public async Task<bool> SendPushNotificationAsync(string expoToken, string title, string body, object? data = null)
        {
            if (string.IsNullOrWhiteSpace(expoToken) || !expoToken.StartsWith("ExponentPushToken"))
                return false;

            var payload = new
            {
                to = expoToken,
                title = title,
                body = body,
                data = data, // Example: new { tripId = 123 }
                sound = "default",
                priority = "high",
                channelId = "default"
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(ExpoApiUrl, payload);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                
                return false;
            }
        }
    }
}