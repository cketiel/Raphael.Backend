namespace Raphael.Notification.Application.DTOs;

public sealed class NotificationRuleRecipientDto
{
    public Guid Id { get; init; }

    public int RecipientTypeId { get; init; }

    public string RecipientTypeName { get; init; } = string.Empty;

    public int PriorityOrder { get; init; }
}