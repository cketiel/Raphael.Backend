using Microsoft.AspNetCore.SignalR;
using Raphael.Shared.DTOs.Realtime;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// Tells the open dispatch screens what just happened.
    /// </summary>
    public interface IDispatchBroadcaster
    {
        Task TripRoutedAsync(int tripId, int vehicleRouteId, DateTime date, int? providerId);

        Task TripUnroutedAsync(int tripId, int vehicleRouteId, DateTime date, int? providerId);

        Task RouteChangedAsync(int vehicleRouteId, DateTime date);

        /// <param name="operatingDate">
        /// The business day the route is working, which is what the screens are grouped by.
        /// Not the date part of the fix: a report taken at two in the morning UTC belongs to the
        /// evening of the day before in most of the country.
        /// </param>
        Task VehiclePositionAsync(VehiclePositionMessage position, DateTime operatingDate);
    }

    /// <inheritdoc />
    /// <remarks>
    /// ⚠️ Every method here swallows its own failures, deliberately.
    ///
    /// This is the same rule the notification publisher follows and for the same reason: a trip
    /// that was routed is routed, whether or not the other screens managed to be told. Letting a
    /// dropped hub connection roll back a dispatcher's work would be trading a real operation
    /// for a cosmetic one. A screen that misses a message is corrected the next time it loads.
    /// </remarks>
    public class DispatchBroadcaster : IDispatchBroadcaster
    {
        private readonly IHubContext<DispatchHub, IDispatchClient> _hub;
        private readonly ILogger<DispatchBroadcaster> _logger;

        public DispatchBroadcaster(
            IHubContext<DispatchHub, IDispatchClient> hub,
            ILogger<DispatchBroadcaster> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public Task TripRoutedAsync(int tripId, int vehicleRouteId, DateTime date, int? providerId) =>
            SafeAsync(
                nameof(TripRoutedAsync),
                async () =>
                {
                    var message = new TripRoutedMessage
                    {
                        TripId = tripId,
                        VehicleRouteId = vehicleRouteId,
                        Date = date.Date
                    };

                    foreach (var group in BoardGroups(providerId, date))
                        await _hub.Clients.Group(group).TripRouted(message);

                    await _hub.Clients
                        .Group(DispatchGroups.Route(vehicleRouteId, date))
                        .RouteChanged(new RouteChangedMessage { VehicleRouteId = vehicleRouteId, Date = date.Date });
                });

        public Task TripUnroutedAsync(int tripId, int vehicleRouteId, DateTime date, int? providerId) =>
            SafeAsync(
                nameof(TripUnroutedAsync),
                async () =>
                {
                    var message = new TripUnroutedMessage
                    {
                        TripId = tripId,
                        VehicleRouteId = vehicleRouteId,
                        Date = date.Date
                    };

                    foreach (var group in BoardGroups(providerId, date))
                        await _hub.Clients.Group(group).TripUnrouted(message);

                    await _hub.Clients
                        .Group(DispatchGroups.Route(vehicleRouteId, date))
                        .RouteChanged(new RouteChangedMessage { VehicleRouteId = vehicleRouteId, Date = date.Date });
                });

        public Task RouteChangedAsync(int vehicleRouteId, DateTime date) =>
            SafeAsync(
                nameof(RouteChangedAsync),
                () => _hub.Clients
                    .Group(DispatchGroups.Route(vehicleRouteId, date))
                    .RouteChanged(new RouteChangedMessage { VehicleRouteId = vehicleRouteId, Date = date.Date }));

        public Task VehiclePositionAsync(VehiclePositionMessage position, DateTime operatingDate) =>
            SafeAsync(
                nameof(VehiclePositionAsync),
                () => _hub.Clients
                    .Group(DispatchGroups.Route(position.VehicleRouteId, operatingDate))
                    .VehiclePosition(position));

        /// <summary>
        /// The provider's own board, and the internal one. A message goes to both because the
        /// two audiences are different: a provider's dispatcher sees only their provider, and an
        /// internal user has no provider and would otherwise see nothing at all.
        /// </summary>
        private static IEnumerable<string> BoardGroups(int? providerId, DateTime date)
        {
            yield return DispatchGroups.Board(DispatchGroups.InternalScope, date);

            if (providerId.HasValue && providerId.Value > 0)
                yield return DispatchGroups.Board(providerId, date);
        }

        private async Task SafeAsync(string operation, Func<Task> send)
        {
            try
            {
                await send();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dispatch board message {Operation} could not be sent.", operation);
            }
        }
    }
}
