using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Shared.Catalog.NotificationRules;

/// <summary>
/// Which notification each business event produces, and for whom.
/// </summary>
/// <remarks>
/// <b>One rule, one audience.</b> A rule holds a single recipient type, because the text
/// is written for the person reading it: a patient is told about their ride, the dispatch
/// office is told which trip to refresh. Mixing two audiences in one rule means one of
/// them reads a message meant for somebody else.
///
/// <para>
/// <b>No conditions.</b> Who is actually notified is decided by which identifiers the
/// event payload carries: no DriverId means the driver is not concerned, no
/// IntegrationId means the trip belongs to nobody outside. See
/// <c>BusinessEventDataKeys</c>.
/// </para>
///
/// <para>
/// <b>Only InApp and Push.</b> The engine delivers those two. SMS, Email and Webhook are
/// declared in the definitions but nothing sends them, so listing them here would be a
/// promise the system does not keep.
/// </para>
///
/// <para>
/// Rules marked <c>Enabled = false</c> exist in the database and are switched off by the
/// synchronisation. They are kept here, rather than deleted, so it stays visible that the
/// decision was to park them and why.
/// </para>
/// </remarks>
public static class NotificationRuleCatalog
{
    public static IReadOnlyList<NotificationRuleCatalogItem> Rules =>
    [
        // =================================================================
        // ACTIVE
        // =================================================================

        #region TRIP_SCHEDULED

        new()
        {
            RuleCode = "RULE_TRIP_SCHEDULED_RIDER",
            RuleName = "Trip Scheduled - Rider",
            Description = "Tells the patient their trip has a vehicle and a time.",
            BusinessEventCode = "TRIP_SCHEDULED",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Medium,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["VIEW_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_TRIP_SCHEDULED_INTEGRATION",
            RuleName = "Trip Scheduled - Integration",
            Description = "Tells the external system that the trip it requested is now scheduled.",
            BusinessEventCode = "TRIP_SCHEDULED",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Integration],
            Channels = [DeliveryChannel.InApp],
            Actions = []
        },

        #endregion

        #region DRIVER_STARTED_TRIP

        new()
        {
            RuleCode = "RULE_DRIVER_STARTED_TRIP_RIDER",
            RuleName = "Driver Started Trip - Rider",
            Description = "Tells the patient the vehicle is on its way, with the estimated arrival.",
            BusinessEventCode = "DRIVER_STARTED_TRIP",
            Type = NotificationType.Alert,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["TRACK_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_STARTED_TRIP_DESKTOP",
            RuleName = "Driver Started Trip - Desktop",
            Description = "Tells the dispatch office that a driver took the trip and is under way.",
            BusinessEventCode = "DRIVER_STARTED_TRIP",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_STARTED_TRIP_INTEGRATION",
            RuleName = "Driver Started Trip - Integration",
            Description = "Tells the external system that its trip is under way.",
            BusinessEventCode = "DRIVER_STARTED_TRIP",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Integration],
            Channels = [DeliveryChannel.InApp],
            Actions = []
        },

        #endregion

        #region TRIP_CANCELLED

        new()
        {
            RuleCode = "RULE_TRIP_CANCELLED_RIDER",
            RuleName = "Trip Cancelled - Rider",
            Description = "Tells the patient their ride is gone, and who dropped it.",
            BusinessEventCode = "TRIP_CANCELLED",
            Type = NotificationType.Alert,
            Priority = NotificationPriority.Critical,
            Severity = NotificationSeverity.Warning,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["VIEW_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_TRIP_CANCELLED_DESKTOP",
            RuleName = "Trip Cancelled - Desktop",
            Description = "Tells the dispatch office to refresh: a trip on screen no longer exists.",
            BusinessEventCode = "TRIP_CANCELLED",
            Type = NotificationType.Alert,
            Priority = NotificationPriority.Critical,
            Severity = NotificationSeverity.Warning,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_TRIP_CANCELLED_DRIVER",
            RuleName = "Trip Cancelled - Driver",
            Description = "Stops a driver already under way from continuing to a trip that no longer exists.",
            BusinessEventCode = "TRIP_CANCELLED",
            Type = NotificationType.Alert,
            Priority = NotificationPriority.Critical,
            Severity = NotificationSeverity.Warning,
            Recipients = [RecipientType.Driver],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_TRIP_CANCELLED_INTEGRATION",
            RuleName = "Trip Cancelled - Integration",
            Description = "Tells the external system that the trip it requested was cancelled.",
            BusinessEventCode = "TRIP_CANCELLED",
            Type = NotificationType.Alert,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Warning,
            Recipients = [RecipientType.Integration],
            Channels = [DeliveryChannel.InApp],
            Actions = []
        },

        #endregion

        #region DRIVER_ROUTE_UPDATED

        new()
        {
            RuleCode = "RULE_DRIVER_ROUTE_UPDATED_DRIVER",
            RuleName = "Route Updated - Driver",
            Description = "Signals Raphael.Driver that the route it has on screen is out of date.",
            BusinessEventCode = "DRIVER_ROUTE_UPDATED",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Driver],
            // ⚠️ InApp only, and deliberately no Push. A signal is worth nothing with the app
            // closed — the driver reloads their schedule when they open it anyway — and
            // vibrating a phone at the wheel for something nobody is meant to read is noise.
            Channels = [DeliveryChannel.InApp],
            Actions = []
        },

        #endregion

        #region TRIP_REACTIVATED

        new()
        {
            RuleCode = "RULE_TRIP_REACTIVATED_RIDER",
            RuleName = "Trip Reactivated - Rider",
            Description = "Tells the patient the ride they were told was gone is back.",
            BusinessEventCode = "TRIP_REACTIVATED",
            Type = NotificationType.Confirmation,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["VIEW_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_TRIP_REACTIVATED_DESKTOP",
            RuleName = "Trip Reactivated - Desktop",
            Description = "Tells the dispatch office a trip is back on the board and needs a route again.",
            BusinessEventCode = "TRIP_REACTIVATED",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Medium,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_TRIP_REACTIVATED_INTEGRATION",
            RuleName = "Trip Reactivated - Integration",
            Description = "Tells the external system the trip it was told had been cancelled is active again.",
            BusinessEventCode = "TRIP_REACTIVATED",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Medium,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Integration],
            Channels = [DeliveryChannel.InApp],
            Actions = []
        },

        // ⚠️ No driver rule on purpose. A trip coming out of cancellation has no route and
        // nobody is on the way to it; the driver is told when it is scheduled again.

        #endregion

        #region WILL_CALL

        new()
        {
            RuleCode = "RULE_WILL_CALL_CREATED_RIDER",
            RuleName = "Will Call Created - Rider",
            Description = "Tells the patient their trip now waits for them to say they are ready.",
            BusinessEventCode = "WILL_CALL_CREATED",
            Type = NotificationType.Confirmation,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp],
            Actions = ["VIEW_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_WILL_CALL_CREATED_DESKTOP",
            RuleName = "Will Call Created - Desktop",
            Description = "Tells the office the trip is waiting on the patient, so nobody dispatches a vehicle for it.",
            BusinessEventCode = "WILL_CALL_CREATED",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Medium,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_WILL_CALL_ACTIVATED_DESKTOP",
            RuleName = "Will Call Activated - Desktop",
            Description = "A patient reported being ready. The office has one hour to get a vehicle there.",
            BusinessEventCode = "WILL_CALL_ACTIVATED",
            Type = NotificationType.ActionRequired,
            Priority = NotificationPriority.Critical,
            Severity = NotificationSeverity.Warning,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_WILL_CALL"]
        },

        new()
        {
            RuleCode = "RULE_WILL_CALL_ACTIVATED_RIDER",
            RuleName = "Will Call Activated - Rider",
            Description = "Confirms the request to a patient somebody else rang on behalf of.",
            BusinessEventCode = "WILL_CALL_ACTIVATED",
            Type = NotificationType.Confirmation,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp],
            Actions = ["VIEW_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_WILL_CALL_ACKNOWLEDGED_RIDER",
            RuleName = "Will Call Acknowledged - Rider",
            Description = "Tells the patient a dispatcher took charge, and by when a vehicle should reach them.",
            BusinessEventCode = "WILL_CALL_ACKNOWLEDGED",
            Type = NotificationType.Confirmation,
            Priority = NotificationPriority.High,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp],
            Actions = ["VIEW_TRIP"]
        },

        #endregion

        #region DRIVER PROGRESS

        new()
        {
            RuleCode = "RULE_DRIVER_ARRIVED_PICKUP_RIDER",
            RuleName = "Driver Arrived - Rider",
            Description = "Tells the patient the vehicle is at the door, so they are not left waiting inside.",
            BusinessEventCode = "DRIVER_ARRIVED_PICKUP",
            Type = NotificationType.Alert,
            Priority = NotificationPriority.Critical,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["TRACK_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_ARRIVED_PICKUP_DESKTOP",
            RuleName = "Driver Arrived - Desktop",
            Description = "Tells the dispatch office the vehicle reached the pickup address.",
            BusinessEventCode = "DRIVER_ARRIVED_PICKUP",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Medium,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_PICKED_UP_PASSENGER_DESKTOP",
            RuleName = "Passenger On Board - Desktop",
            Description = "Tells the dispatch office the patient boarded and the vehicle is heading to the dropoff.",
            BusinessEventCode = "DRIVER_PICKED_UP_PASSENGER",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Information,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_COMPLETED_TRIP_RIDER",
            RuleName = "Trip Completed - Rider",
            Description = "Tells the patient the trip is done and invites them to rate the driver.",
            BusinessEventCode = "DRIVER_COMPLETED_TRIP",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Medium,
            Severity = NotificationSeverity.Success,
            Recipients = [RecipientType.Rider],
            Channels = [DeliveryChannel.InApp, DeliveryChannel.Push],
            Actions = ["RATE_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_COMPLETED_TRIP_DESKTOP",
            RuleName = "Trip Completed - Desktop",
            Description = "Tells the dispatch office the trip closed and the vehicle is free.",
            BusinessEventCode = "DRIVER_COMPLETED_TRIP",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Success,
            Recipients = [RecipientType.DesktopUser],
            Channels = [DeliveryChannel.InApp],
            Actions = ["OPEN_TRIP"]
        },

        new()
        {
            RuleCode = "RULE_DRIVER_COMPLETED_TRIP_INTEGRATION",
            RuleName = "Trip Completed - Integration",
            Description = "Tells the external system its trip was carried out.",
            BusinessEventCode = "DRIVER_COMPLETED_TRIP",
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Success,
            Recipients = [RecipientType.Integration],
            Channels = [DeliveryChannel.InApp],
            Actions = []
        },

        #endregion

        // =================================================================
        // PARKED
        //
        // Present in the database and switched off by the synchronisation.
        // Two reasons only: superseded by TRIP_CANCELLED, or waiting for code
        // that publishes the event. An active rule for an event nobody
        // publishes is not harmless: WILL_CALL_ACTIVATED_DRIVER would have
        // written a recipient-less row on every Will Call.
        // =================================================================

        #region Superseded by TRIP_CANCELLED

        Parked("RULE_DRIVER_CANCELLED_TRIP_RIDER", "DRIVER_CANCELLED_TRIP",
            "Superseded by TRIP_CANCELLED, which carries the actor in its payload."),

        Parked("RULE_DRIVER_CANCELLED_TRIP_DESKTOP", "DRIVER_CANCELLED_TRIP",
            "Superseded by TRIP_CANCELLED."),

        Parked("RULE_DISPATCHER_CANCELLED_TRIP_RIDER", "DISPATCHER_CANCELLED_TRIP",
            "Superseded by TRIP_CANCELLED."),

        Parked("RULE_DISPATCHER_CANCELLED_TRIP_DRIVER", "DISPATCHER_CANCELLED_TRIP",
            "Superseded by TRIP_CANCELLED."),

        Parked("RULE_DISPATCHER_CANCELLED_TRIP_PROVIDER", "DISPATCHER_CANCELLED_TRIP",
            "Superseded by TRIP_CANCELLED. Providers are out of scope for now."),

        // RULE_RIDER_CANCELLED_TRIP_DESKTOP is not listed. It was declared here but never
        // reached the database, and parking a rule that does not exist would only create
        // a row for the pleasure of switching it off. TRIP_CANCELLED covers it.

        #endregion

        #region Waiting for the code that publishes the event

        Parked("RULE_WILL_CALL_ACTIVATED_DRIVER", "WILL_CALL_ACTIVATED",
            "A Will Call has no driver assigned yet: this rule could only ever produce an empty notification."),

        // RULE_DRIVER_RUNNING_LATE_RIDER is not listed, and must not be: its event
        // DRIVER_RUNNING_LATE does not exist in the business event catalog at all. A rule
        // pointing at a missing event aborts the whole synchronisation, which is why the
        // rule catalog could not be synchronised before this. DELAYED_ARRIVAL_DETECTED is
        // the event that covers lateness.

        Parked("RULE_DELAYED_ARRIVAL_DETECTED_DRIVER", "DELAYED_ARRIVAL_DETECTED",
            "Nothing detects delayed arrivals yet."),

        Parked("RULE_ETA_CHANGED_DRIVER", "ETA_CHANGED",
            "Nothing publishes ETA changes yet."),

        Parked("RULE_ROUTE_MODIFIED_RIDER", "ROUTE_MODIFIED",
            "Nothing publishes route changes yet."),

        // RULE_DRIVER_ROUTE_UPDATED_DRIVER is no longer parked: it is now the signal that
        // tells Raphael.Driver its route is out of date. See the DRIVER_ROUTE_UPDATED region.

        Parked("RULE_DISPATCHER_ASSIGNED_TRIP_PROVIDER", "DISPATCHER_ASSIGNED_TRIP",
            "Providers are out of scope for now."),

        Parked("RULE_DISPATCHER_REASSIGNED_TRIP_PROVIDER", "DISPATCHER_REASSIGNED_TRIP",
            "Providers are out of scope for now."),

        Parked("RULE_BOOKING_CREATED_DESKTOP", "BOOKING_CREATED",
            "Nothing publishes booking events yet."),

        Parked("RULE_BOOKING_UPDATED_DESKTOP", "BOOKING_UPDATED",
            "Nothing publishes booking events yet."),

        Parked("RULE_BOOKING_CANCELLED_DESKTOP", "BOOKING_CANCELLED",
            "Nothing publishes booking events yet."),

        Parked("RULE_SYNCHRONIZATION_STARTED_DESKTOP", "SYNCHRONIZATION_STARTED",
            "Nothing publishes synchronisation events yet."),

        Parked("RULE_SYNCHRONIZATION_COMPLETED_DESKTOP", "SYNCHRONIZATION_COMPLETED",
            "Nothing publishes synchronisation events yet."),

        Parked("RULE_SYNCHRONIZATION_FAILED_DESKTOP", "SYNCHRONIZATION_FAILED",
            "Nothing publishes synchronisation events yet."),

        #endregion
    ];

    /// <summary>
    /// A rule that exists in the database and must be switched off, with the reason why.
    /// Recipients and channels are left empty: a parked rule has no shape worth keeping.
    /// </summary>
    private static NotificationRuleCatalogItem Parked(
        string ruleCode,
        string businessEventCode,
        string reason)
    {
        return new NotificationRuleCatalogItem
        {
            RuleCode = ruleCode,
            RuleName = ruleCode,
            Description = $"Disabled. {reason}",
            BusinessEventCode = businessEventCode,
            Type = NotificationType.Notice,
            Priority = NotificationPriority.Low,
            Severity = NotificationSeverity.Information,
            Recipients = [],
            Channels = [],
            Actions = [],
            Enabled = false
        };
    }
}
