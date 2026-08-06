namespace Raphael.Notification.Application.DTOs;

public sealed class NotificationRuleChannelDto
{
    public Guid Id { get; init; }

    public int ChannelId { get; init; }

    public string ChannelName { get; init; } = string.Empty;

    public int PriorityOrder { get; init; }

    public bool IsRequired { get; init; }
}