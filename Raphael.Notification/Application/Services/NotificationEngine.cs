using Raphael.Notification.Application.Delivery;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Helpers;
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


            foreach (var recipient in notification.Recipients)
            {
                // One DTO per recipient, not one shared by all of them. The audience is
                // told about its own row and nothing else.
                var dto =
                    MapToDto(notification, recipient);

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



    /// <summary>
    /// The notification as the given recipient must receive it in real time.
    /// </summary>
    /// <remarks>
    /// Two things this used to get wrong, and both mattered. The recipient row went out
    /// without its <c>Id</c>, so a client could not acknowledge a notification it had
    /// just received live: it had to reload the whole inbox first. And every audience
    /// was included, which put the recipient identifiers of a patient inside the notice
    /// broadcast to the dispatch office.
    /// </remarks>
    private static NotificationDto MapToDto(
        Raphael.Shared.Entities.Notifications.Notification notification,
        Raphael.Shared.Entities.Notifications.NotificationRecipient recipient)
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
                [
                    new NotificationRecipientDto
                    {
                        Id = recipient.Id,
                        RecipientId = recipient.RecipientId,
                        RecipientType = recipient.RecipientType.Name,
                        IsBroadcast =
                            UserIdentifierConverter.IsDesktopAudience(
                                recipient.RecipientId),
                        Status = recipient.Status.Name,
                        DeliveredAtUtc = recipient.DeliveredAtUtc,
                        ViewedAtUtc = recipient.ViewedAtUtc,
                        AcknowledgedAtUtc = recipient.AcknowledgedAtUtc
                    }
                ],

            Actions =
                notification.Actions
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new NotificationActionDto
                    {
                        Id = a.Id,
                        ActionCode = a.ActionCode,
                        SortOrder = a.SortOrder,
                        IsPrimary = a.IsPrimary
                    })
                    .ToList(),

            Metadata =
                notification.Metadata
                    .ToDictionary(m => m.Key, m => m.Value)
        };
    }
}