using Raphael.Notification.Application.DTOs;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Mappers;

public static class NotificationRuleMapper
{
    public static NotificationRuleDto ToDto(NotificationRule rule)
    {
        return new NotificationRuleDto
        {
            Id = rule.Id,

            Code = rule.Code,

            Name = rule.Name,

            Description = rule.Description,

            BusinessEventDefinitionId = rule.BusinessEventDefinitionId,

            BusinessEventCode = rule.BusinessEventDefinition.Code,

            NotificationTypeId = rule.TypeId,

            NotificationTypeName = rule.NotificationType.Name,

            PriorityId = rule.PriorityId,

            PriorityName = rule.Priority.Name,

            SeverityId = rule.SeverityId,

            SeverityName = rule.Severity.Name,

            IsActive = rule.IsActive,

            Recipients = rule.Recipients
                .OrderBy(x => x.PriorityOrder)
                .Select(ToDto)
                .ToList(),

            Channels = rule.Channels
                .OrderBy(x => x.PriorityOrder)
                .Select(ToDto)
                .ToList(),

            Actions = rule.Actions
                .OrderBy(x => x.Order)
                .Select(ToDto)
                .ToList(),

            Conditions = rule.Conditions
                .OrderBy(x => x.Order)
                .Select(ToDto)
                .ToList()
        };
    }

    public static NotificationRuleRecipientDto ToDto(NotificationRuleRecipient recipient)
    {
        return new NotificationRuleRecipientDto
        {
            Id = recipient.Id,

            RecipientTypeId = recipient.RecipientTypeId,

            RecipientTypeName = recipient.RecipientType.Name,

            PriorityOrder = recipient.PriorityOrder
        };
    }

    public static NotificationRuleChannelDto ToDto(NotificationRuleChannel channel)
    {
        return new NotificationRuleChannelDto
        {
            Id = channel.Id,

            ChannelId = channel.ChannelId,

            ChannelName = channel.Channel.Name,

            PriorityOrder = channel.PriorityOrder,

            IsRequired = channel.IsRequired
        };
    }

    public static NotificationRuleActionDto ToDto(NotificationRuleAction action)
    {
        return new NotificationRuleActionDto
        {
            Id = action.Id,

            ActionCode = action.ActionCode,

            Parameters = action.Parameters,

            Order = action.Order
        };
    }

    public static NotificationRuleConditionDto ToDto(NotificationRuleCondition condition)
    {
        return new NotificationRuleConditionDto
        {
            Id = condition.Id,

            Field = condition.Field,

            Operator = condition.Operator,

            Value = condition.Value,

            Order = condition.Order
        };
    }
}