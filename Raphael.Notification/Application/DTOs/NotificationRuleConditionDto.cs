namespace Raphael.Notification.Application.DTOs;

public sealed class NotificationRuleConditionDto
{
    public Guid Id { get; init; }

    public string Field { get; init; } = string.Empty;

    public string Operator { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public int Order { get; init; }
}