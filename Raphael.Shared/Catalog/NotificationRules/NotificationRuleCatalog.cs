using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Catalog.NotificationRules;

public static class NotificationRuleCatalog
{
    public static IReadOnlyList<NotificationRuleCatalogItem> Rules =>
    [
        new()
        {
            RuleCode = "RULE_TRIP_SCHEDULED_RIDER",

            RuleName = "Trip Scheduled - Rider",

            Description = "Notifies the Rider when a trip has been scheduled.",

            BusinessEventCode = "TRIP_SCHEDULED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },
    ];
}