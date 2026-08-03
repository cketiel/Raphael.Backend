using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Raphael.Notification.Application.Commands.CreateNotification;
using Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;
using Raphael.Notification.Application.Commands.MarkNotificationViewed;
using Raphael.Notification.Application.Commands.ProcessBusinessEvent;
using Raphael.Notification.Application.Interfaces.Engine;
using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Rules;
using Raphael.Notification.Application.Queries.GetNotificationById;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;
using Raphael.Notification.Application.Services;
using Raphael.Notification.Application.Interfaces.Factories;

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

        services.AddScoped<ProcessBusinessEventHandler>();

        services.AddScoped<CreateNotificationHandler>();

        services.AddScoped<GetNotificationByIdHandler>();

        services.AddScoped<GetRecipientNotificationsHandler>();

        services.AddScoped<MarkNotificationViewedHandler>();
        services.AddScoped<MarkNotificationAcknowledgedHandler>();
        services.AddScoped<
            IBusinessEventPublisher,
            BusinessEventPublisher>();

        services.AddScoped<
            INotificationEngine,
            NotificationEngine>();

        services.AddScoped<
            INotificationRuleResolver,
            NotificationRuleResolver>();

        services.AddScoped<
            INotificationRuleEvaluator,
            NotificationRuleEvaluator>();

        services.AddScoped<
            INotificationFactory,
            NotificationFactory>();

        return services;
    }
}