using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Catalog.NotificationRules;

public static class NotificationRuleCatalog
{
    public static IReadOnlyList<NotificationRuleCatalogItem> Rules =>
    [

        #region RIDER NOTIFICATIONS


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
                DeliveryChannel.InApp,
                DeliveryChannel.Sms,
                DeliveryChannel.Email
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },


        new()
        {
            RuleCode = "RULE_DRIVER_STARTED_TRIP_RIDER",

            RuleName = "Driver Started Trip - Rider",

            Description = "Notifies the rider that the driver has started the trip.",

            BusinessEventCode = "DRIVER_STARTED_TRIP",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp,
                DeliveryChannel.Push
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },


        new()
        {
            RuleCode = "RULE_DRIVER_ARRIVED_PICKUP_RIDER",

            RuleName = "Driver Arrived Pickup - Rider",

            Description = "Notifies the rider that the driver has arrived at the pickup location.",

            BusinessEventCode = "DRIVER_ARRIVED_PICKUP",

            Type = NotificationType.Alert,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp,
                DeliveryChannel.Push
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },


        new()
        {
            RuleCode = "RULE_DRIVER_COMPLETED_TRIP_RIDER",

            RuleName = "Driver Completed Trip - Rider",

            Description = "Notifies the rider that the trip has been completed.",

            BusinessEventCode = "DRIVER_COMPLETED_TRIP",

            Type = NotificationType.Confirmation,

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


        new()
        {
            RuleCode = "RULE_DRIVER_CANCELLED_TRIP_RIDER",

            RuleName = "Driver Cancelled Trip - Rider",

            Description = "Notifies the rider that the assigned driver cancelled the trip.",

            BusinessEventCode = "DRIVER_CANCELLED_TRIP",

            Type = NotificationType.Warning,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

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


        new()
        {
            RuleCode = "RULE_DISPATCHER_CANCELLED_TRIP_RIDER",

            RuleName = "Dispatcher Cancelled Trip - Rider",

            Description = "Notifies the rider that the dispatcher cancelled the trip.",

            BusinessEventCode = "DISPATCHER_CANCELLED_TRIP",

            Type = NotificationType.Warning,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

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

               /* new()
        {
            RuleCode = "RULE_DRIVER_RUNNING_LATE_RIDER",

            RuleName = "Driver Running Late - Rider",

            Description = "Notifies the rider that the driver is running late.",

            BusinessEventCode = "DRIVER_RUNNING_LATE",

            Type = NotificationType.Alert,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp,
                DeliveryChannel.Push
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },*/


        new()
        {
            RuleCode = "RULE_ROUTE_MODIFIED_RIDER",

            RuleName = "Route Modified - Rider",

            Description = "Notifies the rider that the trip route has been modified.",

            BusinessEventCode = "ROUTE_MODIFIED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp,
                DeliveryChannel.Push,
                DeliveryChannel.Email
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },


        new()
        {
            RuleCode = "RULE_WILL_CALL_ACTIVATED_RIDER",

            RuleName = "Will Call Activated - Rider",

            Description = "Notifies the rider that the trip has been activated as Will Call.",

            BusinessEventCode = "WILL_CALL_ACTIVATED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp//,
                //DeliveryChannel.Push
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },


        new()
        {
            RuleCode = "RULE_WILL_CALL_ACKNOWLEDGED_RIDER",

            RuleName = "Will Call Acknowledged - Rider",

            Description = "Notifies the rider that the Will Call request has been acknowledged.",

            BusinessEventCode = "WILL_CALL_ACKNOWLEDGED",

            Type = NotificationType.Confirmation,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Rider
            ],

            Channels =
            [
                DeliveryChannel.InApp//,
                //DeliveryChannel.Push
            ],

            Actions =
            [
                "VIEW_TRIP"
            ]
        },

        #endregion

    ];
}