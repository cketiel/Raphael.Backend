using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Interfaces.Engine;
using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Factories;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Application.Interfaces.Rules;
using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationEngine
    : INotificationEngine
{
    private readonly INotificationRuleResolver _ruleResolver;
    private readonly INotificationRuleEvaluator _ruleEvaluator;
    private readonly INotificationFactory _notificationFactory;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly NotificationDeliveryService _deliveryService;

    public NotificationEngine(
        INotificationRuleResolver ruleResolver,
        INotificationRuleEvaluator ruleEvaluator,
        INotificationFactory notificationFactory,
        INotificationRepository notificationRepository,
        INotificationDispatcher dispatcher,
        NotificationDeliveryService deliveryService)
    {
        _ruleResolver = ruleResolver;
        _ruleEvaluator = ruleEvaluator;
        _notificationFactory = notificationFactory;
        _notificationRepository = notificationRepository;
        _dispatcher = dispatcher;
        _deliveryService = deliveryService;
    }


    public async Task ProcessAsync(
        BusinessEventContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);


        var rules = await _ruleResolver.ResolveAsync(
            context,
            cancellationToken);


        foreach (var rule in rules)
        {
            var applies =
                await _ruleEvaluator.EvaluateAsync(
                    rule,
                    context,
                    cancellationToken);


            if (!applies)
            {
                continue;
            }


            var notification =
                _notificationFactory.Create(
                    rule,
                    context);


            // A rule whose audience is not concerned by this particular event resolves to
            // no recipient: the trip belongs to no integration, or no driver had taken it
            // yet. Storing that row would fill the table with notifications nobody can
            // ever read, which is exactly what the retention policy is fighting.
            if (notification.Recipients.Count == 0)
            {
                continue;
            }


            await _notificationRepository.AddAsync(
                notification,
                cancellationToken);

            await _notificationRepository.SaveChangesAsync(cancellationToken);


            var dto =
                MapToDto(notification);



            foreach (var recipient in notification.Recipients)
            {
                // In-App / SignalR
                await _dispatcher.SendNotificationAsync(
                    recipient.RecipientId,
                    recipient.RecipientType,
                    dto,
                    cancellationToken);

                // Push
                var pushChannels =
                    rule.Channels
                        .Where(x => x.Channel == DeliveryChannel.Push)
                        .ToList();

                if (pushChannels.Any())
                {
                    await _deliveryService.DeliverAsync(
                        notification,
                        recipient,
                        pushChannels,
                        cancellationToken);
                }

                /*await _deliveryService.DeliverAsync(
                notification,
                recipient,
                rule.Channels,
                cancellationToken);*/
            }
        }
    }



    private static NotificationDto MapToDto(
        Raphael.Shared.Entities.Notifications.Notification notification)
    {
        return new NotificationDto
        {
            Id = notification.Id,

            BusinessEventCode =
                notification.BusinessEventCode,

            Priority =
                notification.Priority.Name,

            Severity =
                notification.Severity.Name,

            Type =
                notification.Type.Name,

            Status =
                notification.Status.Name,

            Title =
                notification.Title,

            Message =
                notification.Message,

            CreatedAtUtc =
                notification.CreatedAtUtc,

            ExpiresAtUtc =
                notification.ExpiresAtUtc,

            Recipients =
                notification.Recipients
                    .Select(r => new NotificationRecipientDto
                    {
                        RecipientId = r.RecipientId,
                        RecipientType = r.RecipientType.Name
                    })
                    .ToList(),

            Actions =
                notification.Actions
                    .Select(a => new NotificationActionDto
                    {
                        ActionCode = a.ActionCode,
                        SortOrder = a.SortOrder,
                        IsPrimary = a.IsPrimary
                    })
                    .ToList()
        };
    }
}