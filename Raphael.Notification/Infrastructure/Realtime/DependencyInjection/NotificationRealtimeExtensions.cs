using Microsoft.Extensions.DependencyInjection;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Infrastructure.Realtime.Services;
using Raphael.Notification.Infrastructure.Realtime.Stores;

namespace Raphael.Notification.Infrastructure.Realtime.DependencyInjection;

public static class NotificationRealtimeExtensions
{
    public static IServiceCollection AddNotificationRealtime(
        this IServiceCollection services)
    {
        services.AddSingleton<IConnectionStore, InMemoryConnectionStore>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();

        return services;
    }
}