using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Domain.Content;

/// <summary>
/// Composes the text of a notification for one business event and one audience.
/// </summary>
/// <remarks>
/// Two rules govern everything here.
///
/// <para>
/// <b>No PHI.</b> No patient name, address or phone number ever reaches a title or a
/// message. A push crosses Expo's and Google's servers and lands on a lock screen that
/// anybody standing nearby can read. What travels is the trip identifier; the app loads
/// the detail once the patient opens it, already authenticated.
/// </para>
///
/// <para>
/// <b>The text is English and the client translates.</b> The stored title and message are
/// what the server pushes, so they are English. Every notification also carries a message
/// key and its parameters, and each application renders the in-app inbox in the language
/// its user picked.
/// </para>
/// </remarks>
public static class NotificationContentBuilder
{
    /// <summary>
    /// Minutes the dispatch office has to get a vehicle to a patient who activated a
    /// Will Call. It is a commitment to the patient, not a display detail.
    /// </summary>
    public const int WillCallCommitmentMinutes = 60;

    /// <param name="operationZone">
    /// Where the trip is operated. Every hour written into a message is wall-clock time
    /// there.
    /// </param>
    /// <remarks>
    /// ⚠️ The zone is passed in rather than read from the machine. This text tells a patient
    /// sitting in a clinic by when a vehicle will reach them; rendered with the server's own
    /// clock it promised an hour that does not exist for anybody involved.
    /// </remarks>
    public static NotificationContent Build(
        string eventCode,
        RecipientType audience,
        BusinessEventContext context,
        NotificationRule rule,
        TimeZoneInfo operationZone)
    {
        var parameters = new Dictionary<string, string>();

        var trip = GetTrip(context);
        var tripId = GetTripId(context, trip);

        if (tripId is not null)
            parameters[NotificationMetadataKeys.TripId] = tripId;

        var tripDate = trip?.Date;
        var tripTime = trip?.FromTime;

        if (tripDate.HasValue)
            parameters[NotificationMetadataKeys.TripDate] = tripDate.Value.ToString("yyyy-MM-dd");

        if (tripTime.HasValue)
            parameters[NotificationMetadataKeys.TripTime] = FormatTime(tripTime.Value);

        var performedBy = GetString(context, BusinessEventDataKeys.PerformedByUserId);

        if (!string.IsNullOrWhiteSpace(performedBy))
            parameters[NotificationMetadataKeys.PerformedByUserId] = performedBy;

        var messageKey = $"notification.{eventCode}.{audience.Code}";

        var (title, message) = Compose(
            eventCode,
            audience,
            context,
            parameters,
            tripId,
            tripDate,
            tripTime,
            rule,
            operationZone);

        parameters[NotificationMetadataKeys.MessageKey] = messageKey;

        return new NotificationContent(
            title,
            message,
            messageKey,
            parameters);
    }

    private static (string Title, string Message) Compose(
        string eventCode,
        RecipientType audience,
        BusinessEventContext context,
        Dictionary<string, string> parameters,
        string? tripId,
        DateTime? tripDate,
        TimeSpan? tripTime,
        NotificationRule rule,
        TimeZoneInfo operationZone)
    {
        var isRider = audience.Id == RecipientType.Rider.Id;
        var whenForRider = DescribeWhen(tripDate, tripTime);
        var tripLabel = tripId is null ? "the trip" : $"trip {tripId}";

        switch (eventCode)
        {
            case BusinessEventCodes.TripScheduled:

                return isRider
                    ? ("Trip Scheduled",
                       $"Your trip{whenForRider} has been scheduled. Open the app to see the details.")
                    : ("Trip Scheduled",
                       $"{Capitalize(tripLabel)}{whenForRider} has been scheduled.");

            case BusinessEventCodes.DriverStartedTrip:

                var minutes = GetTravelMinutes(context);

                if (minutes.HasValue)
                    parameters[NotificationMetadataKeys.EtaMinutes] =
                        minutes.Value.ToString();

                if (isRider)
                {
                    return ("Driver On The Way",
                        minutes.HasValue
                            ? $"Your driver is on the way to your pickup location and should arrive in about {minutes} minutes."
                            : "Your driver is on the way to your pickup location.");
                }

                return ("Trip Started",
                    $"A driver has started {tripLabel} and is heading to the pickup location.");

            case BusinessEventCodes.DriverArrivedPickup:

                return isRider
                    ? ("Driver Arrived",
                       "Your driver has arrived at the pickup location.")
                    : ("Driver Arrived",
                       $"The driver has arrived at the pickup location for {tripLabel}.");

            case BusinessEventCodes.DriverPickedUpPassenger:

                return ("Passenger On Board",
                    $"{Capitalize(tripLabel)} is on the way to the dropoff location.");

            case BusinessEventCodes.DriverCompletedTrip:

                return isRider
                    ? ("Trip Completed",
                       "Your trip has been completed. You can now rate your driver.")
                    : ("Trip Completed",
                       $"{Capitalize(tripLabel)} has been completed.");

            case BusinessEventCodes.TripCancelled:

                var cancelledBy = GetString(context, BusinessEventDataKeys.CancelledBy);

                if (cancelledBy is not null)
                    parameters[NotificationMetadataKeys.CancelledBy] = cancelledBy;

                if (audience.Id == RecipientType.Driver.Id)
                {
                    return ("Trip Cancelled",
                        $"{Capitalize(tripLabel)} has been cancelled. You no longer need to complete it.");
                }

                if (isRider)
                {
                    return ("Trip Cancelled",
                        $"Your trip{whenForRider} has been cancelled{DescribeActorSuffix(cancelledBy)}.");
                }

                return ("Trip Cancelled",
                    $"{Capitalize(tripLabel)}{whenForRider} was cancelled{DescribeActorSuffix(cancelledBy)}.");

            case BusinessEventCodes.TripReactivated:

                return isRider
                    ? ("Trip Reactivated",
                       $"Your trip{whenForRider} is active again. We will let you know once a vehicle is assigned.")
                    : ("Trip Reactivated",
                       $"{Capitalize(tripLabel)}{whenForRider} was reactivated after being cancelled, and needs a route again.");

            case BusinessEventCodes.WillCallCreated:

                // ⚠️ Nothing is switched off here: the trip goes back to waiting for the
                // patient. Saying "cancelled" or "deactivated" to a patient would read as
                // losing their ride, which is the opposite of what happened.
                return isRider
                    ? ("Will Call",
                       "Your trip is now a Will Call. Tell us when you are ready and the office will send a vehicle.")
                    : ("Will Call",
                       $"{Capitalize(tripLabel)} is a Will Call: it waits until the patient says they are ready. No vehicle is due until then.");

            case BusinessEventCodes.WillCallActivated:

                var activatedAt = GetDateTime(context, BusinessEventDataKeys.WillCallActivatedAtUtc)
                                  ?? DateTime.UtcNow;

                var deadline = activatedAt.AddMinutes(WillCallCommitmentMinutes);

                parameters[NotificationMetadataKeys.WillCallActivatedAtUtc] = FormatUtc(activatedAt);
                parameters[NotificationMetadataKeys.WillCallDeadlineUtc] = FormatUtc(deadline);

                // Carried so that a dispatcher acknowledging this notice can reach the
                // patient without another lookup.
                var riderId = GetString(context, BusinessEventDataKeys.RiderId);

                if (!string.IsNullOrWhiteSpace(riderId))
                    parameters[NotificationMetadataKeys.RiderId] = riderId;

                return isRider
                    ? ("Will Call Activated",
                       $"We received your request. A vehicle should reach you by {FormatOperationTime(deadline, operationZone)}.")
                    : ("Will Call Activated",
                       $"A patient is ready for pickup on {tripLabel}. A vehicle must reach them by {FormatOperationTime(deadline, operationZone)}.");

            case BusinessEventCodes.WillCallAcknowledged:

                // The hour is counted from the activation, never from the moment a
                // dispatcher happened to look at the screen.
                var acknowledgedActivation =
                    GetDateTime(context, BusinessEventDataKeys.WillCallActivatedAtUtc)
                    ?? DateTime.UtcNow;

                var acknowledgedDeadline =
                    acknowledgedActivation.AddMinutes(WillCallCommitmentMinutes);

                parameters[NotificationMetadataKeys.WillCallActivatedAtUtc] =
                    FormatUtc(acknowledgedActivation);

                parameters[NotificationMetadataKeys.WillCallDeadlineUtc] =
                    FormatUtc(acknowledgedDeadline);

                return ("Will Call Confirmed",
                    $"Our dispatch office is arranging your ride. A vehicle should reach you by {FormatOperationTime(acknowledgedDeadline, operationZone)}.");

            default:

                return (rule.Name, rule.Description);
        }
    }

    /// <summary>
    /// " on 03/12/2026 at 09:15", or the part of it we actually know.
    /// </summary>
    private static string DescribeWhen(DateTime? date, TimeSpan? time)
    {
        if (!date.HasValue)
            return string.Empty;

        var text = $" on {date.Value:MM/dd/yyyy}";

        if (time.HasValue)
            text += $" at {FormatTime(time.Value)}";

        return text;
    }

    private static string DescribeActorSuffix(string? cancelledBy)
    {
        return cancelledBy switch
        {
            CancelledByTypes.Dispatcher => " by the dispatch office",
            CancelledByTypes.Driver => " by the assigned driver",
            CancelledByTypes.Rider => " at the passenger's request",
            CancelledByTypes.Facility => " by the requesting facility",
            CancelledByTypes.Integrator => " by the requesting organization",
            CancelledByTypes.Bot => " through customer service",
            _ => string.Empty
        };
    }

    private static Trip? GetTrip(BusinessEventContext context)
    {
        return context.Data.TryGetValue(BusinessEventDataKeys.Trip, out var value)
            ? value as Trip
            : null;
    }

    private static string? GetTripId(BusinessEventContext context, Trip? trip)
    {
        if (context.Data.TryGetValue(BusinessEventDataKeys.TripId, out var value) &&
            value is not null)
        {
            return value.ToString();
        }

        return trip?.Id.ToString();
    }

    private static int? GetTravelMinutes(BusinessEventContext context)
    {
        if (!context.Data.TryGetValue(BusinessEventDataKeys.Travel, out var value))
            return null;

        return value switch
        {
            TimeSpan travel => (int)Math.Ceiling(travel.TotalMinutes),
            _ => null
        };
    }

    private static string? GetString(BusinessEventContext context, string key)
    {
        return context.Data.TryGetValue(key, out var value)
            ? value?.ToString()
            : null;
    }

    private static DateTime? GetDateTime(BusinessEventContext context, string key)
    {
        if (!context.Data.TryGetValue(key, out var value))
            return null;

        return value switch
        {
            DateTime moment => moment,
            string text when DateTime.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }

    private static string FormatTime(TimeSpan time)
        => $"{time.Hours:D2}:{time.Minutes:D2}";

    private static string FormatUtc(DateTime moment)
        => DateTime.SpecifyKind(moment, DateTimeKind.Utc).ToString("O");

    /// <summary>
    /// An hour a patient can act on: wall-clock time where the trip is operated.
    /// </summary>
    /// <remarks>
    /// ⚠️ Never the machine's own zone. This used to call ToLocalTime(), so the deadline a
    /// patient read was whatever hour it happened to be wherever the API was hosted.
    /// </remarks>
    private static string FormatOperationTime(DateTime utcMoment, TimeZoneInfo operationZone)
        => TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utcMoment, DateTimeKind.Utc),
                operationZone)
            .ToString("h:mm tt");

    private static string Capitalize(string text)
        => string.IsNullOrEmpty(text)
            ? text
            : char.ToUpperInvariant(text[0]) + text[1..];
}
