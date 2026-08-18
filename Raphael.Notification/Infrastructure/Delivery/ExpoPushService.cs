using Raphael.Notification.Application.Interfaces.Delivery;
using Raphael.Shared.DTOs;
using System.Net.Http.Json;

namespace Raphael.Notification.Infrastructure.Delivery;

public sealed class ExpoPushService : IExpoPushService
{
    private readonly HttpClient _httpClient;

    private const string ExpoApiUrl =
        "https://exp.host/--/api/v2/push/send";

    public ExpoPushService(
        HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ExpoPushResult> SendAsync(
        string expoToken,
        string title,
        string body,
        object? data = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(expoToken))
        {
            return new ExpoPushResult
            {
                Success = false,
                ErrorMessage = "Push token is empty."
            };
        }

        var cleanToken =
            expoToken
                .Replace("\"", "")
                .Trim();

        var messages = new[]
        {
            new
            {
                to = cleanToken,
                title,
                body,
                data,
                sound = "default"
            }
        };

        try
        {
            var response =
                await _httpClient.PostAsJsonAsync(
                    ExpoApiUrl,
                    messages,
                    cancellationToken);

            var rawContent =
                await response.Content.ReadAsStringAsync(
                    cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new ExpoPushResult
                {
                    Success = false,
                    RawResponse = rawContent,
                    ErrorMessage =
                        $"HTTP Error: {response.StatusCode}"
                };
            }

            return new ExpoPushResult
            {
                Success = true,
                RawResponse = rawContent
            };
        }
        catch (Exception ex)
        {
            return new ExpoPushResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}