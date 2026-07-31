using Microsoft.Extensions.DependencyInjection;
using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Application.Interfaces.Persistence;

using Raphael.Notification.Domain.Engine;
using Raphael.Notification.Infrastructure.Delivery;
using Raphael.Notification.Infrastructure.Persistence.Repositories;


namespace Raphael.Notification.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRaphaelNotification(
        this IServiceCollection services)
    {
        /*
         * Repositories
         */

        services.AddScoped<INotificationRepository, NotificationRepository>();

        services.AddScoped<INotificationRuleRepository, NotificationRuleRepository>();

        services.AddScoped<IBusinessEventDefinitionRepository, BusinessEventDefinitionRepository>();



        /*
         * Rule Engine
         */
        services.AddScoped<NotificationRuleEngine>();

        services.AddScoped< ConditionEvaluator>();

        services.AddScoped<RecipientResolver>();

        services.AddScoped< ChannelResolver>();

        services.AddScoped< PriorityResolver>();

        services.AddScoped<SeverityResolver>();

        services.AddScoped<TypeResolver>();

        /*services.AddScoped<INotificationRuleEngine, NotificationRuleEngine>();

        services.AddScoped<IConditionEvaluator, ConditionEvaluator>();

        services.AddScoped<IRecipientResolver, RecipientResolver>();

        services.AddScoped<IChannelResolver, ChannelResolver>();

        services.AddScoped<IPriorityResolver, PriorityResolver>();

        services.AddScoped<ISeverityResolver, SeverityResolver>();

        services.AddScoped<ITypeResolver, TypeResolver>();*/



        /*
         * Delivery Engine
         */

        services.AddScoped< NotificationSenderFactory>();
        //services.AddScoped<INotificationSenderFactory, NotificationSenderFactory>();

        services.AddScoped<INotificationSender, InAppSender>();

        services.AddScoped<INotificationSender, PushSender>();

        services.AddScoped<INotificationSender, EmailSender>();

        services.AddScoped<INotificationSender, SmsSender>();

        services.AddScoped<INotificationSender, WebhookSender>();


        return services;
    }
}