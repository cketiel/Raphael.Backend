using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Interfaces.Engine;
using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Factories;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Application.Interfaces.Realtime;
using Raphael.Notification.Application.Interfaces.Rules;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationEngine
    : INotificationEngine
{
    private readonly INotificationRuleResolver _ruleResolver;
    private readonly INotificationRuleEvaluator _ruleEvaluator;
    private readonly INotificationFactory _notificationFactory;
    private readonly INotificationRepository _notificationRepository;
    private readonly INotificationDispatcher _dispatcher;


    public NotificationEngine(
        INotificationRuleResolver ruleResolver,
        INotificationRuleEvaluator ruleEvaluator,
        INotificationFactory notificationFactory,
        INotificationRepository notificationRepository,
        INotificationDispatcher dispatcher)
    {
        _ruleResolver = ruleResolver;
        _ruleEvaluator = ruleEvaluator;
        _notificationFactory = notificationFactory;
        _notificationRepository = notificationRepository;
        _dispatcher = dispatcher;
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


            await _notificationRepository.AddAsync(
                notification,
                cancellationToken);

            await _notificationRepository.SaveChangesAsync(cancellationToken);


            var dto =
                MapToDto(notification);



            foreach (var recipient in notification.Recipients)
            {
                await _dispatcher.SendNotificationAsync(
                    recipient.RecipientId,
                    dto,
                    cancellationToken);
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