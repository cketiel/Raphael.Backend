using Microsoft.Extensions.DependencyInjection;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Infrastructure.Persistence;
using Raphael.Notification.Infrastructure.Persistence.Repositories;

namespace Raphael.Notification.Infrastructure.DependencyInjection;

public static class NotificationInfrastructureExtensions
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services)
    {
        services.AddScoped<
            INotificationRepository,
            NotificationRepository>();

        services.AddScoped<
            INotificationRuleRepository,
            NotificationRuleRepository>();

        services.AddScoped<
            IBusinessEventRepository,
            BusinessEventRepository>();

        services.AddScoped<
            IBusinessEventDefinitionRepository,
            BusinessEventDefinitionRepository>();

        services.AddScoped<
            INotificationRecipientRepository,
            NotificationRecipientRepository>();

        services.AddScoped<
            INotificationDeliveryRepository,
            NotificationDeliveryRepository>();

        return services;
    }
}