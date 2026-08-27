using Raphael.Shared.Entities;

namespace Raphael.Api.Services.Notifications
{
    /// <summary>
    /// Publishes the business events of a trip's life cycle.
    /// </summary>
    /// <remarks>
    /// Single place where the payload of a trip event is assembled, because who gets
    /// notified is decided by which identifiers the payload carries, and getting that
    /// wrong in one of the six cancellation paths would be invisible until a patient
    /// stopped being told their ride was gone.
    ///
    /// <para>
    /// Every method must be called <b>after</b> SaveChanges, and after CommitAsync when
    /// there is a transaction: announcing a change that can still be rolled back means
    /// telling a patient about a trip that will not exist.
    /// </para>
    ///
    /// <para>
    /// No method throws. A notification that fails must never turn a successful
    /// cancellation into an error the caller reports back to the patient.
    /// </para>
    /// </remarks>
    public interface ITripNotificationPublisher
    {
        /// <summary>
        /// A trip entered or left the route of a driver who is already out working it.
        /// </summary>
        /// <remarks>
        /// This publishes a <b>signal</b>, not a notice: Raphael.Driver acts on it and deletes
        /// it, and it never reaches an inbox or the bell. It covers the three ways a route
        /// changes under a driver — a dispatcher adds a trip, a dispatcher takes one off, or a
        /// trip on the route is cancelled — because from the driver's seat they are one fact:
        /// the schedule on screen is wrong.
        ///
        /// <para>
        /// ⚠️ Nothing is published unless the Pull-out of that route and day is performed.
        /// Before that the driver has no live route to correct.
        /// </para>
        ///
        /// <para>
        /// A cancellation must still call <see cref="TripCancelledAsync"/> as well: that one
        /// is the notice the driver reads, this one is the instruction the app obeys.
        /// </para>
        /// </remarks>
        /// <param name="routeChange">See <see cref="Shared.Definitions.Notifications.RouteChangeTypes"/>.</param>
        /// <param name="vehicleRouteId">
        /// Route the trip left, for the case where it has already been detached from the trip
        /// and <c>trip.VehicleRouteId</c> is null by the time this is called.
        /// </param>
        Task DriverRouteUpdatedAsync(
            Trip trip,
            string routeChange,
            int? vehicleRouteId = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// A trip was cancelled, from any of its six origins.
        /// </summary>
        /// <param name="trip">The trip, already cancelled and saved.</param>
        /// <param name="cancelledBy">Kind of actor. See <see cref="Shared.Definitions.Notifications.CancelledByTypes"/>.</param>
        /// <param name="statusBeforeCancellation">
        /// Status the trip had before being cancelled. Decides whether the assigned
        /// driver is pushed: only a trip already under way needs the driver to stop.
        /// </param>
        /// <param name="reason">Reason given, when there is one.</param>
        Task TripCancelledAsync(
            Trip trip,
            string cancelledBy,
            string statusBeforeCancellation,
            string? reason = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// A cancelled trip was put back in service.
        /// </summary>
        /// <remarks>
        /// The patient was told their ride was gone, so they are told it is back. The
        /// driver is not: the trip has no route at this point.
        /// </remarks>
        Task TripReactivatedAsync(
            Trip trip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The trip became a Will Call: it waits for the patient to say they are ready.
        /// </summary>
        /// <remarks>
        /// ⚠️ The opposite of an activation, not of a notification. Nothing is switched
        /// off — a Will Call comes into existence for this trip, whether a dispatcher undid
        /// an activation or turned an ordinary trip into one.
        /// </remarks>
        Task WillCallCreatedAsync(
            Trip trip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// A patient reported being ready on a Will Call trip. The office has one hour
        /// from <paramref name="activatedAtUtc"/> to get a vehicle there.
        /// </summary>
        /// <param name="notifyRider">
        /// False when the patient triggered it themselves from Raphael.Rider: they
        /// already saw the confirmation. True when somebody did it on their behalf.
        /// </param>
        Task WillCallActivatedAsync(
            Trip trip,
            DateTime activatedAtUtc,
            bool notifyRider,
            CancellationToken cancellationToken = default);

        /// <summary>The trip got a route and a vehicle assigned.</summary>
        Task TripScheduledAsync(
            Trip trip,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// The driver took the trip and is heading to the pickup address.
        /// </summary>
        /// <param name="travel">Estimated time to the pickup, when it is known.</param>
        Task DriverStartedTripAsync(
            Trip trip,
            TimeSpan? travel,
            CancellationToken cancellationToken = default);

        /// <summary>The driver reached the pickup address.</summary>
        Task DriverArrivedPickupAsync(
            Trip trip,
            CancellationToken cancellationToken = default);

        /// <summary>The patient boarded and the vehicle is heading to the dropoff.</summary>
        Task DriverPickedUpPassengerAsync(
            Trip trip,
            CancellationToken cancellationToken = default);

        /// <summary>The driver left the patient at the destination.</summary>
        Task DriverCompletedTripAsync(
            Trip trip,
            CancellationToken cancellationToken = default);
    }
}
