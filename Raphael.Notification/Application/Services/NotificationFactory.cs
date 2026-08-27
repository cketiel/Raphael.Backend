using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Factories;
using Raphael.Notification.Domain.Content;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Notifications;
using Raphael.Shared.Time;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationFactory
    : INotificationFactory
{
    private readonly IOperationClock _clock;

    public NotificationFactory(IOperationClock clock)
    {
        _clock = clock;
    }

    public NotificationModel Create(
        NotificationRule rule,
        BusinessEventContext context)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(context);

        var eventCode = rule.BusinessEventDefinition.Code;

        var audiences = rule.Recipients
            .Select(x => x.RecipientType)
            .ToList();

        //
        // Content
        //
        // Rules are written per audience (RULE_<EVENT>_RIDER, RULE_<EVENT>_DESKTOP), so
        // the first recipient type is the audience this text is addressed to. A patient
        // is told about their ride; the office is told which trip to refresh.
        //
        var audience = audiences.FirstOrDefault() ?? RecipientType.System;

        // Every hour written into the text is wall-clock time where the trip is operated:
        // the patient reading "by 3:45 PM" is sitting at the pickup address, not next to
        // the server. A trip with no provider is one the broker runs itself.
        var operationZone = _clock.ZoneFor(
            (context.Data.TryGetValue(BusinessEventDataKeys.Trip, out var value)
                ? value as Trip
                : null)?.ProviderId);

        var content = NotificationContentBuilder.Build(
            eventCode,
            audience,
            context,
            rule,
            operationZone);

        // A signal is for an application, not for a person: it ages out in an hour instead
        // of the audience window, because the app deletes it as soon as it acts on it and
        // the ones left over are the ones nobody consumed.
        var isSignal = content.Parameters.ContainsKey(NotificationMetadataKeys.Signal);

        var notification = new NotificationModel(
            businessEventCode: eventCode,
            priority: rule.Priority,
            severity: rule.Severity,
            type: rule.NotificationType,
            title: content.Title,
            message: content.Message,
            expiresAtUtc: NotificationRetentionPolicy.ResolveExpiry(
                DateTime.UtcNow,
                audiences,
                isSignal));

        //
        // Recipients
        //

        foreach (var ruleRecipient in rule.Recipients)
        {
            var recipientId = ResolveRecipientId(
                ruleRecipient.RecipientType,
                context);

            // No identifier in the payload means this audience is not concerned by this
            // particular event. That is how "only if the trip belongs to that
            // integration" and "only if the trip is under way" are expressed, without a
            // single rule condition.
            if (recipientId == Guid.Empty)
                continue;

            // Nobody is notified of their own action. The dispatcher who cancelled
            // already saw the confirmation on screen.
            if (context.PerformedByUserId.HasValue &&
                context.PerformedByUserId.Value == recipientId)
            {
                continue;
            }

            notification.Recipients.Add(
                new NotificationRecipient(
                    notification.Id,
                    recipientId,
                    ruleRecipient.RecipientType));
        }

        //
        // Metadata: what the client applications need to render the text in their own
        // language, and to open the right screen. Identifiers only, never PHI.
        //

        foreach (var parameter in content.Parameters)
        {
            notification.Metadata.Add(
                new NotificationMetadata(
                    notification.Id,
                    parameter.Key,
                    parameter.Value));
        }

        //
        // Actions
        //

        foreach (var action in rule.Actions.OrderBy(x => x.Order))
        {
            notification.Actions.Add(
                new NotificationAction(
                    notification.Id,
                    action.ActionCode,
                    action.Order,
                    action.Order == 1));
        }

        return notification;
    }

    /// <summary>
    /// Turns the identifier carried in the payload into the recipient Guid, with the
    /// recipient type baked in. Two people of different kinds sharing a number must not
    /// share an inbox.
    /// </summary>
    private static Guid ResolveRecipientId(
        RecipientType recipientType,
        BusinessEventContext context)
    {
        //
        // The dispatch office is addressed as a whole: one stored notification that
        // every dispatcher reads, instead of one row per user.
        //
        if (recipientType.Id == RecipientType.DesktopUser.Id &&
            !context.Data.ContainsKey(BusinessEventDataKeys.DesktopUserId))
        {
            return UserIdentifierConverter.DesktopAudience;
        }

        var key = recipientType.Code switch
        {
            "RIDER" => BusinessEventDataKeys.RiderId,
            "DRIVER" => BusinessEventDataKeys.DriverId,
            "DESKTOP_USER" => BusinessEventDataKeys.DesktopUserId,
            "INTEGRATION" => BusinessEventDataKeys.IntegrationId,
            _ => null
        };

        if (key is null)
            return Guid.Empty;

        if (!context.Data.TryGetValue(key, out var raw) || raw is null)
            return Guid.Empty;

        if (!int.TryParse(raw.ToString(), out var id) || id <= 0)
            return Guid.Empty;

        return UserIdentifierConverter.ToGuid(id, recipientType);
    }
}
