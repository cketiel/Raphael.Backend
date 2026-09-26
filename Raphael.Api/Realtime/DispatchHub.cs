using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Raphael.Api.Services.Auth;
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

        public DispatchHub(ICallerRoles roles)
        {
            _roles = roles;
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

            await Groups.AddToGroupAsync(Context.ConnectionId, DispatchGroups.Route(vehicleRouteId, day));
        }

        public async Task UnwatchRoute(int vehicleRouteId, string date)
        {
            if (vehicleRouteId <= 0 || !TryParseDay(date, out var day)) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, DispatchGroups.Route(vehicleRouteId, day));
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
