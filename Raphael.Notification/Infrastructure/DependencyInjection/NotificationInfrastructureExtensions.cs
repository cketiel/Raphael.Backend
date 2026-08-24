using Microsoft.Extensions.DependencyInjection;
using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Application.Interfaces.Delivery;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Infrastructure.Delivery;
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

        services.AddScoped<
            IPushTokenProvider,
            PushTokenProvider>();

        // Named on purpose. Raphael.Api declares its own IExpoPushService, and the HTTP
        // client factory derives the client name from the type name alone, ignoring the
        // namespace: registering both unnamed throws at startup and the application never
        // comes up. Two Expo clients for one provider is duplication worth removing, but
        // that is a refactor of RiderService, not a fix for a service that will not boot.
        services.AddHttpClient<IExpoPushService, ExpoPushService>("Notifications.ExpoPush");
        services.AddScoped<INotificationSender, PushSender>();
        services.AddScoped<INotificationSender, InAppSender>();

        return services;
    }
}