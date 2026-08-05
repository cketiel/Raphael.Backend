using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Catalog;

public sealed class NotificationRuleDefinition
{
    public required string BusinessEventCode { get; init; }

    public required string RuleCode { get; init; }

    public required string RuleName { get; init; }

    public required string Description { get; init; }

    public required NotificationPriority Priority { get; init; }

    public required NotificationSeverity Severity { get; init; }

    public required NotificationType NotificationType { get; init; }

    public List<NotificationRecipientDefinition> Recipients { get; } = new();

    public List<NotificationChannelDefinition> Channels { get; } = new();

    public List<NotificationActionDefinition> Actions { get; } = new();
}