using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Interfaces.Factories;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Notifications;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Notification.Application.Services;

public sealed class NotificationFactory
    : INotificationFactory
{
    public NotificationModel Create(
        NotificationRule rule,
        BusinessEventContext context)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentNullException.ThrowIfNull(context);

        var trip = context.Data.TryGetValue("Trip", out var tripValue)
            ? tripValue as Trip
            : null;

        string title;
        string message;

        switch (context.EventCode)
        {
            case "TRIP_SCHEDULED":

                title = "Trip Scheduled";

                message =
                    $"Your trip has been scheduled for {trip?.Date:MM/dd/yyyy} at {trip?.FromTime:hh\\:mm}. Name: {trip.Customer.FullName}. Pickup: {trip.PickupAddress}. Dropoff: {trip.DropoffAddress}";

                break;

            case "DRIVER_STARTED_TRIP":

                title = "Driver Started Trip";

                var travel = context.Data.TryGetValue(
                    "Travel",
                    out var travelValue)
                    ? travelValue as TimeSpan?
                    : null;

                if (travel.HasValue)
                {
                    var totalMinutes = (int)Math.Ceiling(
                        travel.Value.TotalMinutes);

                    message =
                        $"Your driver is on the way to your pickup location. Your transportation will arrive in {totalMinutes} minutes.";
                }
                else
                {
                    message =
                        "Your driver is on the way to your pickup location.";
                }

                break;

            case "DRIVER_ARRIVED_PICKUP":

                title = "Driver Arrived";

                message =
                    "Your driver has arrived at the pickup location.";

                break;

            case "DRIVER_COMPLETED_TRIP":

                title = "Trip Completed";

                message =
                    "Your trip has been completed successfully. You can now rate the driver.";

                break;

            case "DRIVER_CANCELLED_TRIP":

                title = "Trip Cancelled";

                message =
                    "The assigned driver cancelled your trip.";

                break;

            case "DISPATCHER_CANCELLED_TRIP":

                title = "Trip Cancelled";

                message =
                    "Your dispatcher cancelled the scheduled trip.";

                break;

            case "WILL_CALL_ACTIVATED":

                title = "Will Call Activated";

                message =
                    "Your return trip is now available to be requested.";

                break;

            case "WILL_CALL_ACKNOWLEDGED":

                title = "Will Call Confirmed";

                message =
                    "Your Will Call request has been received and is being processed.";

                break;

            default:

                title = rule.Name;

                message = rule.Description;

                break;
        }

        var notification = new NotificationModel(
            businessEventCode: rule.BusinessEventDefinition.Code,
            priority: rule.Priority,
            severity: rule.Severity,
            type: rule.NotificationType,
            title: title,
            message: message);

        //
        // Recipients
        //

        foreach (var ruleRecipient in rule.Recipients)
        {
            Guid recipientId = Guid.Empty;

            switch (ruleRecipient.RecipientType.Code)
            {
                case "RIDER":

                    if (context.Data.TryGetValue("RiderId", out var riderValue))
                    {
                        recipientId =
                            UserIdentifierConverter.ToGuid(
                                Convert.ToInt32(riderValue));
                    }

                    break;

                case "DRIVER":

                    if (context.Data.TryGetValue("DriverId", out var driverValue))
                    {
                        recipientId =
                            UserIdentifierConverter.ToGuid(
                                Convert.ToInt32(driverValue));
                    }

                    break;

                case "DESKTOP_USER":

                    if (context.Data.TryGetValue("DesktopUserId", out var desktopValue))
                    {
                        recipientId =
                            UserIdentifierConverter.ToGuid(
                                Convert.ToInt32(desktopValue));
                    }

                    break;

                case "INTEGRATION":

                    if (context.Data.TryGetValue("IntegrationId", out var integrationValue))
                    {
                        recipientId =
                            UserIdentifierConverter.ToGuid(
                                Convert.ToInt32(integrationValue));
                    }

                    break;
            }

            if (recipientId == Guid.Empty)
                continue;

            notification.Recipients.Add(
                new NotificationRecipient(
                    notification.Id,
                    recipientId,
                    ruleRecipient.RecipientType));
        }

        //
        // Actions
        //

        foreach (var action in rule.Actions.OrderBy(x => x.Order))
        {
            notification.Actions.Add(
                new NotificationAction(
                    notification.Id,
                    action.ActionCode,
                    action.Order,
                    action.Order == 1));
        }

        return notification;
    }
    private static Guid ResolveRecipientId(
    RecipientType recipientType,
    BusinessEventContext context)
    {
        return recipientType.Code switch
        {
            "RIDER"
                => GetGuid(context, "RiderId"),

            "DRIVER"
                => GetGuid(context, "DriverId"),

            "DESKTOP_USER"
                => GetGuid(context, "DesktopUserId"),

            "SYSTEM"
                => GetGuid(context, "SystemUserId"),

            "INTEGRATION"
                => GetGuid(context, "IntegrationId"),

            _ => Guid.Empty
        };
    }

    private static Guid GetGuid(
        BusinessEventContext context,
        string key)
    {
        if (!context.Data.TryGetValue(key, out var value))
            return Guid.Empty;

        return value is Guid guid
            ? guid
            : Guid.Empty;
    }
}