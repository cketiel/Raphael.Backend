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

        #region DESKTOP NOTIFICATIONS

        new()
        {
            RuleCode = "RULE_WILL_CALL_ACTIVATED_DESKTOP",

            RuleName = "Will Call Activated - Desktop",

            Description = "Notifies dispatchers that a rider has activated a Will Call request.",

            BusinessEventCode = "WILL_CALL_ACTIVATED",

            Type = NotificationType.Alert,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_WILL_CALL"
            ]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_CANCELLED_TRIP_DESKTOP",

            RuleName = "Driver Cancelled Trip - Desktop",

            Description = "Notifies dispatchers that a driver cancelled a trip.",

            BusinessEventCode = "DRIVER_CANCELLED_TRIP",

            Type = NotificationType.Alert,

            Priority = NotificationPriority.Critical,

            Severity = NotificationSeverity.Warning,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_TRIP"
            ]
        },

        new()
        {
            RuleCode = "RULE_BOOKING_CREATED_DESKTOP",

            RuleName = "Booking Created - Desktop",

            Description = "Notifies office users that a new booking has been created.",

            BusinessEventCode = "BOOKING_CREATED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_BOOKING"
            ]
        },

        new()
        {
            RuleCode = "RULE_BOOKING_UPDATED_DESKTOP",

            RuleName = "Booking Updated - Desktop",

            Description = "Notifies office users that a booking has been updated.",

            BusinessEventCode = "BOOKING_UPDATED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_BOOKING"
            ]
        },

        new()
        {
            RuleCode = "RULE_BOOKING_CANCELLED_DESKTOP",

            RuleName = "Booking Cancelled - Desktop",

            Description = "Notifies office users that a booking has been cancelled.",

            BusinessEventCode = "BOOKING_CANCELLED",

            Type = NotificationType.Warning,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_BOOKING"
            ]
        },

        new()
        {
            RuleCode = "RULE_SYNCHRONIZATION_STARTED_DESKTOP",

            RuleName = "Synchronization Started",

            Description = "Notifies that an integration synchronization has started.",

            BusinessEventCode = "SYNCHRONIZATION_STARTED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Low,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_SYNCHRONIZATION"
            ]
        },

        new()
        {
            RuleCode = "RULE_SYNCHRONIZATION_COMPLETED_DESKTOP",

            RuleName = "Synchronization Completed",

            Description = "Notifies that an integration synchronization completed successfully.",

            BusinessEventCode = "SYNCHRONIZATION_COMPLETED",

            Type = NotificationType.Confirmation,

            Priority = NotificationPriority.Low,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp
            ],

            Actions =
            [
                "OPEN_SYNCHRONIZATION"
            ]
        },

        new()
        {
            RuleCode = "RULE_SYNCHRONIZATION_FAILED_DESKTOP",

            RuleName = "Synchronization Failed",

            Description = "Notifies that an integration synchronization failed.",

            BusinessEventCode = "SYNCHRONIZATION_FAILED",

            Type = NotificationType.Alert,

            Priority = NotificationPriority.Critical,

            Severity = NotificationSeverity.Critical,

            Recipients =
            [
                RecipientType.DesktopUser
            ],

            Channels =
            [
                DeliveryChannel.InApp,
                DeliveryChannel.Email
            ],

            Actions =
            [
                "OPEN_SYNCHRONIZATION"
            ]
        },

        new()
        {
            RuleCode = "RULE_DISPATCHER_ASSIGNED_TRIP_PROVIDER",

            RuleName = "Dispatcher Assigned Trip - Provider",

            Description = "Notifies the Provider that a Dispatcher assigned a trip.",

            BusinessEventCode = "DISPATCHER_ASSIGNED_TRIP",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
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
            RuleCode = "RULE_DISPATCHER_CANCELLED_TRIP_PROVIDER",

            RuleName = "Dispatcher Cancelled Trip - Provider",

            Description = "Notifies the Provider that a Dispatcher cancelled a trip.",

            BusinessEventCode = "DISPATCHER_CANCELLED_TRIP",

            Type = NotificationType.Warning,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

            Recipients =
            [
                RecipientType.DesktopUser
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
            RuleCode = "RULE_DISPATCHER_REASSIGNED_TRIP_PROVIDER",

            RuleName = "Dispatcher Reassigned Trip - Provider",

            Description = "Notifies the Provider that a Dispatcher reassigned a trip.",

            BusinessEventCode = "DISPATCHER_REASSIGNED_TRIP",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.DesktopUser
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

        #endregion

        #region DRIVER NOTIFICATIONS

        new()
        {
            RuleCode = "RULE_DRIVER_ROUTE_UPDATED_DRIVER",

            RuleName = "Driver Route Updated",

            Description = "Notifies the driver that the assigned route or trip information has changed.",

            BusinessEventCode = "DRIVER_ROUTE_UPDATED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Driver
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
            RuleCode = "RULE_DELAYED_ARRIVAL_DETECTED_DRIVER",

            RuleName = "Delayed Arrival Detected",

            Description = "Notifies the driver that the estimated arrival time exceeds the expected threshold.",

            BusinessEventCode = "DELAYED_ARRIVAL_DETECTED",

            Type = NotificationType.Warning,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

            Recipients =
            [
                RecipientType.Driver
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
            RuleCode = "RULE_ETA_CHANGED_DRIVER",

            RuleName = "ETA Changed",

            Description = "Notifies the driver that the estimated arrival time has changed.",

            BusinessEventCode = "ETA_CHANGED",

            Type = NotificationType.Notice,

            Priority = NotificationPriority.Medium,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Driver
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
            RuleCode = "RULE_WILL_CALL_ACTIVATED_DRIVER",

            RuleName = "Will Call Activated",

            Description = "Notifies the driver that a Will Call trip is ready for dispatch.",

            BusinessEventCode = "WILL_CALL_ACTIVATED",

            Type = NotificationType.Alert,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Information,

            Recipients =
            [
                RecipientType.Driver
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
            RuleCode = "RULE_DISPATCHER_CANCELLED_TRIP_DRIVER",

            RuleName = "Dispatcher Cancelled Trip",

            Description = "Notifies the driver that the dispatcher cancelled the assigned trip.",

            BusinessEventCode = "DISPATCHER_CANCELLED_TRIP",

            Type = NotificationType.Warning,

            Priority = NotificationPriority.High,

            Severity = NotificationSeverity.Warning,

            Recipients =
            [
                RecipientType.Driver
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

        #endregion

    ];
}