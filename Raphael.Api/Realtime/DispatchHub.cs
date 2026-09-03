using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Globalization;
using System.Security.Claims;

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
        /// Which board this connection is allowed on, taken from the token and never from the
        /// caller. A user who belongs to a provider hears that provider; one who does not is
        /// internal and hears the scope every message is also published to.
        /// </summary>
        private string CallerScope()
        {
            var raw = Context.User?.FindFirst("ProviderId")?.Value;

            return int.TryParse(raw, out var providerId) && providerId > 0
                ? providerId.ToString(CultureInfo.InvariantCulture)
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
