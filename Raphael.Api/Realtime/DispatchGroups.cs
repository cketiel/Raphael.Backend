using System;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// The names of the groups a dispatch screen listens on.
    /// </summary>
    /// <remarks>
    /// Two axes, because the screen watches two things at once: the day's backlog, which is
    /// shared by every dispatcher working that day, and the one route whose stops are on screen.
    ///
    /// The board is segmented by provider from the start. The office broadcast used by the
    /// notification inbox is not — that is an open finding in the backlog — and this channel
    /// deliberately does not inherit it. A dispatcher who belongs to a provider joins only that
    /// provider's board; an internal user, who has no provider, joins the one every message is
    /// also sent to.
    /// </remarks>
    public static class DispatchGroups
    {
        /// <summary>The scope an internal user without a provider listens on.</summary>
        public const string InternalScope = "all";

        public static string Board(string scope, DateTime date) =>
            $"board:{scope}:{Day(date)}";

        public static string Board(int? providerId, DateTime date) =>
            Board(providerId.HasValue ? providerId.Value.ToString() : InternalScope, date);

        public static string Route(int vehicleRouteId, DateTime date) =>
            $"route:{vehicleRouteId}:{Day(date)}";

        /// <summary>
        /// The day part of a group name. Invariant and date-only: a group name is an identifier,
        /// and one that changed shape with the server's culture would silently split an office
        /// into two halves that never hear each other.
        /// </summary>
        public static string Day(DateTime date) =>
            date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    }
}
