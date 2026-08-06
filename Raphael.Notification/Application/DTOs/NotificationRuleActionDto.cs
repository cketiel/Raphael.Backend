namespace Raphael.Notification.Application.DTOs;

public sealed class NotificationRuleActionDto
{
    public Guid Id { get; init; }

    public string ActionCode { get; init; } = string.Empty;

    public string? Parameters { get; init; }

    public int Order { get; init; }
}