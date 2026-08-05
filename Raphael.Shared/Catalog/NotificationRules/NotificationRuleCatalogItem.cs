using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Catalog.NotificationRules;

public sealed class NotificationRuleCatalogItem
{
    public string RuleCode { get; init; } = string.Empty;

    public string RuleName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string BusinessEventCode { get; init; } = string.Empty;

    public NotificationPriority Priority { get; init; }

    public NotificationSeverity Severity { get; init; }

    public NotificationType Type { get; init; }

    public IReadOnlyList<RecipientType> Recipients { get; init; } = [];

    public IReadOnlyList<DeliveryChannel> Channels { get; init; } = [];

    public IReadOnlyList<string> Actions { get; init; } = [];

    public IReadOnlyList<NotificationRuleConditionCatalogItem> Conditions { get; init; } = [];

    public bool Enabled { get; init; } = true;
}