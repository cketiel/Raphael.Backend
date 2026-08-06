namespace Raphael.Notification.Application.DTOs;

public sealed class UpdateNotificationRuleDto
{
    public Guid Id { get; init; }

    public int NotificationTypeId { get; init; }

    public int PriorityId { get; init; }

    public int SeverityId { get; init; }

    public bool IsActive { get; init; }

    public List<NotificationRuleRecipientDto> Recipients { get; init; } = [];

    public List<NotificationRuleChannelDto> Channels { get; init; } = [];

    public List<NotificationRuleActionDto> Actions { get; init; } = [];

    public List<NotificationRuleConditionDto> Conditions { get; init; } = [];
}