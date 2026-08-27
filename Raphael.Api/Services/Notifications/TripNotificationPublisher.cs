using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Services;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services.Notifications
{
    /// <inheritdoc cref="ITripNotificationPublisher"/>
    public class TripNotificationPublisher : ITripNotificationPublisher
    {
        private readonly RaphaelContext _context;
        private readonly NotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<TripNotificationPublisher> _logger;

        public TripNotificationPublisher(
            RaphaelContext context,
            NotificationService notificationService,
            ICurrentUserService currentUserService,
            ILogger<TripNotificationPublisher> logger)
        {
            _context = context;
            _notificationService = notificationService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task TripCancelledAsync(
            Trip trip,
            string cancelledBy,
            string statusBeforeCancellation,
            string? reason = null,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            data[BusinessEventDataKeys.CancelledBy] = cancelledBy;

            if (!string.IsNullOrWhiteSpace(reason))
                data[BusinessEventDataKeys.CancellationReason] = reason;

            // The assigned driver is only told when the trip was already under way.
            // Warning a driver about a trip they had not started yet is noise; letting
            // one drive to a pickup that no longer exists is a wasted vehicle.
            if (IsUnderWay(statusBeforeCancellation))
            {
                var driverId = await ResolveDriverIdAsync(trip, cancellationToken);

                if (driverId.HasValue)
                    data[BusinessEventDataKeys.DriverId] = driverId.Value;
            }

            await PublishAsync(
                BusinessEventCodes.TripCancelled,
                trip,
                data,
                ResolveCancellationAuthor(trip, cancelledBy),
                cancellationToken);

            // A cancelled trip that was on a route is, from the driver's seat, a trip that
            // left their route. The notice above is what they read; this is what makes the
            // app reload a schedule that no longer matches reality.
            //
            // Both go out even when the trip was under way, and that is not a duplicate: one
            // is addressed to a person and shows in their inbox, the other is addressed to
            // the application and never does.
            await DriverRouteUpdatedAsync(
                trip,
                RouteChangeTypes.Removed,
                cancellationToken: cancellationToken);
        }

        public async Task TripReactivatedAsync(
            Trip trip,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            // ⚠️ No driver, deliberately. A trip coming out of cancellation has no route:
            // there is nobody on the way to it to be told, and the driver hears about it
            // when it is scheduled again.
            await PublishAsync(
                BusinessEventCodes.TripReactivated,
                trip,
                data,
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        /// <summary>
        /// The trip went back to waiting for the patient to say they are ready.
        /// </summary>
        /// <remarks>
        /// ⚠️ Not the opposite of a notification, the opposite of an activation. Nothing is
        /// switched off: a Will Call comes into existence for this trip, which is why the
        /// event is <c>WILL_CALL_CREATED</c> and not a "deactivated".
        /// </remarks>
        public async Task WillCallCreatedAsync(
            Trip trip,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            // Same reason as an activation: an integration is told when the state of its
            // trip changes for its own purposes, not about the office's back and forth.
            data.Remove(BusinessEventDataKeys.IntegrationId);

            await PublishAsync(
                BusinessEventCodes.WillCallCreated,
                trip,
                data,
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        public async Task WillCallActivatedAsync(
            Trip trip,
            DateTime activatedAtUtc,
            bool notifyRider,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            data[BusinessEventDataKeys.WillCallActivatedAtUtc] = activatedAtUtc;

            // An integration does not care that a patient rang: it is told when the
            // state of its trip changes, not about every step in between.
            data.Remove(BusinessEventDataKeys.IntegrationId);

            // RiderId stays in the payload even when the patient is not to be notified:
            // it is what lets a dispatcher acknowledging this notice reach them back.
            // The patient is left out as the author of the action instead.
            var author = notifyRider
                ? ResolveDesktopAuthor()
                : UserIdentifierConverter.ToGuid(trip.CustomerId, RecipientType.Rider);

            await PublishAsync(
                BusinessEventCodes.WillCallActivated,
                trip,
                data,
                author,
                cancellationToken);
        }

        public async Task TripScheduledAsync(
            Trip trip,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            // The office does not need telling: whoever routed the trip is looking at
            // the screen where it just moved.
            await PublishAsync(
                BusinessEventCodes.TripScheduled,
                trip,
                data,
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        public async Task DriverStartedTripAsync(
            Trip trip,
            TimeSpan? travel,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            if (travel.HasValue)
                data[BusinessEventDataKeys.Travel] = travel.Value;

            await PublishAsync(
                BusinessEventCodes.DriverStartedTrip,
                trip,
                data,
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        public async Task DriverArrivedPickupAsync(
            Trip trip,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            // The integration was already told the trip started; every intermediate hop
            // would be noise it cannot act on.
            data.Remove(BusinessEventDataKeys.IntegrationId);

            await PublishAsync(
                BusinessEventCodes.DriverArrivedPickup,
                trip,
                data,
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        public async Task DriverPickedUpPassengerAsync(
            Trip trip,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            var data = BuildTripData(trip);

            // Only the dispatch office follows this one: the patient is in the vehicle
            // and does not need their phone to tell them so.
            data.Remove(BusinessEventDataKeys.RiderId);
            data.Remove(BusinessEventDataKeys.IntegrationId);

            await PublishAsync(
                BusinessEventCodes.DriverPickedUpPassenger,
                trip,
                data,
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        public async Task DriverCompletedTripAsync(
            Trip trip,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            await PublishAsync(
                BusinessEventCodes.DriverCompletedTrip,
                trip,
                BuildTripData(trip),
                ResolveDesktopAuthor(),
                cancellationToken);
        }

        /// <summary>
        /// Base payload of any trip event. Which identifiers it carries decides who is
        /// notified, so removing a key is how an audience is deliberately left out.
        /// </summary>
        private Dictionary<string, object?> BuildTripData(Trip trip)
        {
            var data = new Dictionary<string, object?>
            {
                [BusinessEventDataKeys.TripId] = trip.Id,
                [BusinessEventDataKeys.Trip] = trip,
                [BusinessEventDataKeys.RiderId] = trip.CustomerId
            };

            if (trip.IntegratorId.HasValue)
                data[BusinessEventDataKeys.IntegrationId] = trip.IntegratorId.Value;

            if (_currentUserService.UserId.HasValue)
                data[BusinessEventDataKeys.PerformedByUserId] = _currentUserService.UserId.Value;

            return data;
        }

        /// <summary>
        /// Publishes, and never lets a notification failure change the outcome of the
        /// operation that triggered it. A cancellation that went through is cancelled,
        /// whether or not anybody managed to be told.
        /// </summary>
        private async Task PublishAsync(
            string eventCode,
            Trip trip,
            Dictionary<string, object?> data,
            Guid? performedBy,
            CancellationToken cancellationToken)
        {
            try
            {
                await _notificationService.PublishAsync(
                    eventCode: eventCode,
                    aggregateId: UserIdentifierConverter.ToGuid(trip.Id),
                    data: data,
                    performedByUserId: performedBy,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to publish {EventCode} for trip {TripId}.",
                    eventCode,
                    trip.Id);
            }
        }

        /// <summary>
        /// Who cancelled, expressed as a recipient identifier so the factory can leave
        /// them out.
        /// </summary>
        /// <remarks>
        /// The recipient type has to match the one the audience is addressed with, or
        /// the comparison silently fails and the person gets notified of their own act:
        /// a driver who just cancelled would be pushed to stop a trip they cancelled.
        /// </remarks>
        private Guid? ResolveCancellationAuthor(Trip trip, string cancelledBy)
        {
            if (cancelledBy == CancelledByTypes.Rider)
            {
                // A patient's token carries CustomerId, not UserId.
                return UserIdentifierConverter.ToGuid(
                    trip.CustomerId,
                    RecipientType.Rider);
            }

            if (!_currentUserService.UserId.HasValue)
                return null;

            var authorType = cancelledBy == CancelledByTypes.Driver
                ? RecipientType.Driver
                : RecipientType.DesktopUser;

            return UserIdentifierConverter.ToGuid(
                _currentUserService.UserId.Value,
                authorType);
        }

        private Guid? ResolveDesktopAuthor()
        {
            if (!_currentUserService.UserId.HasValue)
                return null;

            return UserIdentifierConverter.ToGuid(
                _currentUserService.UserId.Value,
                RecipientType.DesktopUser);
        }

        /// <inheritdoc />
        public async Task DriverRouteUpdatedAsync(
            Trip trip,
            string routeChange,
            int? vehicleRouteId = null,
            CancellationToken cancellationToken = default)
        {
            if (trip is null) return;

            try
            {
                var routeId = vehicleRouteId ?? trip.VehicleRouteId;

                // No route means no driver watching a schedule that just went stale.
                if (!routeId.HasValue)
                    return;

                var driverId = await ResolveRouteDriverIdAsync(routeId.Value, cancellationToken);

                if (!driverId.HasValue)
                    return;

                // ⚠️ Only once the driver is out of the garage. Before Pull-out there is no
                // live route on their screen to correct, and a signal then would interrupt
                // somebody who is not working yet for a change they will see anyway when
                // they start their shift.
                if (!await HasPulledOutAsync(routeId.Value, trip.Date, cancellationToken))
                    return;

                var data = new Dictionary<string, object?>
                {
                    [BusinessEventDataKeys.TripId] = trip.Id,
                    [BusinessEventDataKeys.Trip] = trip,
                    [BusinessEventDataKeys.DriverId] = driverId.Value,
                    [BusinessEventDataKeys.RouteChange] = routeChange
                };

                // Deliberately no PerformedByUserId. It exists so nobody is told about their
                // own action, and here the actor is a dispatcher while the recipient is a
                // driver: they can never be the same person, and carrying it would only put
                // a dispatcher identifier on a driver's device.

                await _notificationService.PublishAsync(
                    eventCode: BusinessEventCodes.DriverRouteUpdated,
                    aggregateId: UserIdentifierConverter.ToGuid(trip.Id),
                    data: data,
                    performedByUserId: null,
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // A signal that does not go out costs the driver one manual refresh. Letting
                // it break the routing operation that caused it would cost a trip.
                _logger.LogError(
                    ex,
                    "Could not publish DRIVER_ROUTE_UPDATED for trip {TripId}.",
                    trip.Id);
            }
        }

        /// <summary>
        /// Driver of a route, by route id rather than through the trip.
        /// </summary>
        /// <remarks>
        /// Needed because a trip taken off a route no longer carries the identifier of the
        /// route it left, and that driver is precisely the one who has to be told.
        /// </remarks>
        private async Task<int?> ResolveRouteDriverIdAsync(
            int vehicleRouteId,
            CancellationToken cancellationToken)
        {
            var driverId = await _context.VehicleRoutes
                .AsNoTracking()
                .Where(x => x.Id == vehicleRouteId)
                .Select(x => (int?)x.DriverId)
                .FirstOrDefaultAsync(cancellationToken);

            return driverId is > 0 ? driverId : null;
        }

        /// <summary>
        /// Whether the driver already started this route for this day.
        /// </summary>
        /// <remarks>
        /// Pull-out is the event that marks leaving the garage. Its <c>Performed</c> flag is
        /// the only thing in the data that says a driver is actually out working the route.
        /// </remarks>
        private async Task<bool> HasPulledOutAsync(
            int vehicleRouteId,
            DateTime? date,
            CancellationToken cancellationToken)
        {
            if (!date.HasValue)
                return false;

            var day = date.Value.Date;

            return await _context.Schedules
                .AsNoTracking()
                .AnyAsync(
                    x => x.VehicleRouteId == vehicleRouteId
                         && x.Name == "Pull-out"
                         && x.Date.HasValue
                         && x.Date.Value.Date == day
                         && x.Performed,
                    cancellationToken);
        }

        private async Task<int?> ResolveDriverIdAsync(
            Trip trip,
            CancellationToken cancellationToken)
        {
            if (!trip.VehicleRouteId.HasValue)
                return null;

            var driverId = await _context.VehicleRoutes
                .AsNoTracking()
                .Where(x => x.Id == trip.VehicleRouteId.Value)
                .Select(x => (int?)x.DriverId)
                .FirstOrDefaultAsync(cancellationToken);

            return driverId is > 0 ? driverId : null;
        }

        /// <summary>
        /// A trip is under way once a driver took it, whether they are still driving to
        /// the pickup or already waiting at the door.
        /// </summary>
        private static bool IsUnderWay(string? status)
        {
            return status == TripStatus.Started
                   || status == TripStatus.Arrived
                   || status == TripStatus.InProgress;
        }
    }
}
