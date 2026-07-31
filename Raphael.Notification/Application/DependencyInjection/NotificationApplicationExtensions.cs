using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Raphael.Notification.Application.Services;

namespace Raphael.Notification.Application.DependencyInjection;

public static class NotificationApplicationExtensions
{
    public static IServiceCollection AddNotificationApplication(
        this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(
            typeof(NotificationApplicationExtensions).Assembly);


        services.AddScoped<NotificationService>();

        services.AddScoped<BusinessEventService>();

        services.AddScoped<NotificationRuleService>();

        services.AddScoped<NotificationEnumerationResolver>();


        return services;
    }
}