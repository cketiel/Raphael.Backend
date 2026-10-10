using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services;
using Raphael.Api.Services.Auth;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Realtime;
using System.Globalization;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// The live channel behind the dispatch board: what one dispatcher does, the others see.
    /// </summary>
    /// <remarks>
    /// Separate from the notification hub on purpose, and not a second copy of it. Everything
    /// that goes through the notification engine is written to the Notifications table and lands
    /// in somebody's bell; a vehicle reporting its position every thirty seconds would fill that
    /// table with rows nobody is meant to read. Nothing here is stored, so there is no retention
    /// window to configure and nothing to purge.
    ///
    /// A screen says what it is looking at — the day, and the route whose stops are open — and
    /// stops listening when it looks elsewhere. It cannot say which provider it belongs to: that
    /// comes from the token, so a client cannot ask to watch another provider's board.
    /// </remarks>
    [Authorize]
    public class DispatchHub : Hub<IDispatchClient>
    {
        private readonly ICallerRoles _roles;
        private readonly RaphaelContext _context;
        private readonly IGpsService _gps;
        private readonly ITripProgress _progress;

        public DispatchHub(ICallerRoles roles, RaphaelContext context, IGpsService gps, ITripProgress progress)
        {
            _roles = roles;
            _context = context;
            _gps = gps;
            _progress = progress;
        }

        /// <summary>
        /// Starts listening to a day's backlog: trips routed and unrouted by anyone else.
        /// </summary>
        public async Task WatchBoard(string date)
        {
            if (!TryParseDay(date, out var day)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Board(CallerScope(), day));
        }

        public async Task UnwatchBoard(string date)
        {
            if (!TryParseDay(date, out var day)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DispatchGroups.Board(CallerScope(), day));
        }

        /// <summary>
        /// Starts listening to one route on one day: its order, its hours, and its vehicle.
        /// </summary>
        public async Task WatchRoute(int vehicleRouteId, string date)
        {
            if (vehicleRouteId <= 0 || !TryParseDay(date, out var day)) return;

            // A clinic follows its own trips through WatchTrip, never a whole route: a route
            // carries other patients, and its vehicle shows where they are picked up.
            if (IsClinic()) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Route(vehicleRouteId, day));
        }

        public async Task UnwatchRoute(int vehicleRouteId, string date)
        {
            if (vehicleRouteId <= 0 || !TryParseDay(date, out var day)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DispatchGroups.Route(vehicleRouteId, day));
        }

        /// <summary>
        /// A clinic starts following one of its own trips: the vehicle's position arrives while the
        /// trip is under way, and only then.
        /// </summary>
        /// <returns>
        /// Null when the trip is not the caller's, or the caller is not a clinic. Otherwise whether the
        /// trip is under way, with the last position reported when it is.
        /// </returns>
        public async Task<WatchTripResult?> WatchTrip(int tripId)
        {
            var integratorId = Context.User is null ? null : _roles.IntegratorIdOf(Context.User);
            if (tripId <= 0 || integratorId is null) return null;

            // The ownership check is ours, not the query filter's: inside a hub the filter has no
            // request to read the caller from.
            var trip = await _context.Trips
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => t.Id == tripId && t.IntegratorId == integratorId)
                .Select(t => new { t.Id, t.Status, t.VehicleRouteId })
                .FirstOrDefaultAsync();

            if (trip is null) return null;

            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Trip(trip.Id));

            if (!TripTracking.IsUnderWay(trip.Status) || trip.VehicleRouteId is null)
            {
                return new WatchTripResult { InProgress = false };
            }

            var latest = await _gps.GetLatestGpsDataAsync(trip.VehicleRouteId.Value);

            // The last fix, with this trip's phase, miles to go and ETAs, as every later fix brings them.
            var position = latest is null
                ? null
                : (await _progress.ForTripsAsync(
                    new[] { trip.Id }, latest.Latitude, latest.Longitude, latest.Speed, latest.Direction, latest.DateTime))
                  .FirstOrDefault();

            return new WatchTripResult { InProgress = true, Position = position };
        }

        /// <summary>
        /// A clinic's trip list starts hearing the status changes of its own trips. The clinic
        /// comes from the token, so a caller cannot ask for another clinic's.
        /// </summary>
        public async Task WatchClinic()
        {
            var integratorId = Context.User is null ? null : _roles.IntegratorIdOf(Context.User);
            if (integratorId is null) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Clinic(integratorId.Value));
        }

        public async Task UnwatchTrip(int tripId)
        {
            if (tripId <= 0) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DispatchGroups.Trip(tripId));
        }

        /// <summary>
        /// Starts listening to the drivers' call-back queue. Office staff only: the queue names
        /// drivers and who is handling them, which is nobody else's business.
        /// </summary>
        public async Task WatchCallRequests()
        {
            if (Context.User is null || !_roles.IsOffice(Context.User)) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.CallRequests(CallerScope()));
        }

        public async Task UnwatchCallRequests()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DispatchGroups.CallRequests(CallerScope()));
        }

        /// <summary>
        /// Which board this connection is allowed on, taken from the token and never from the
        /// caller. A user who belongs to a provider hears that provider; one who does not is
        /// internal and hears the scope every message is also published to.
        /// </summary>
        /// <remarks>
        /// ⚠️ The login issues the claim as <c>UserProviderId</c>. This used to read
        /// <c>ProviderId</c>, which no token carries, so every user landed in the internal scope
        /// and heard every provider.
        /// </remarks>
        private bool IsClinic() =>
            Context.User is not null && _roles.IntegratorIdOf(Context.User).HasValue;

        private string CallerScope()
        {
            var providerId = Context.User is null ? null : _roles.ProviderIdOf(Context.User);

            return providerId.HasValue
                ? providerId.Value.ToString(CultureInfo.InvariantCulture)
                : DispatchGroups.InternalScope;
        }

        private static bool TryParseDay(string date, out DateTime day) =>
            DateTime.TryParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out day);
    }
}
