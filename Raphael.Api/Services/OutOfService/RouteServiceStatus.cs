using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.OutOfService;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services.OutOfService
{
    /// <summary>A route that cannot take trips on that day. Answered as a 409.</summary>
    public sealed class RouteNotRoutableException : InvalidOperationException
    {
        public const string OutOfService = "route_out_of_service";

        public const string Suspended = "route_suspended";

        public RouteNotRoutableException(string message, string code) : base(message) => Code = code;

        public string Code { get; }
    }

    /// <summary>
    /// Whether a route can take trips on a day: not while its vehicle or its driver is out of
    /// service (RE-053), nor on a day inside one of its suspensions.
    /// </summary>
    /// <remarks>
    /// ⚠️ Suspensions used to be stored and never read: a suspended route was routed like any other.
    /// This is the one place both are checked, so every way of putting a trip on a route asks it.
    /// </remarks>
    public static class RouteServiceStatus
    {
        /// <summary>Throws <see cref="RouteNotRoutableException"/> when the route cannot take a trip on <paramref name="date"/>.</summary>
        public static async Task EnsureRoutableAsync(RaphaelContext context, VehicleRoute route, DateTime date, CancellationToken ct = default)
        {
            var outOfService = await OpenRecordsFor(context, route.VehicleId, route.DriverId)
                .Select(r => r.Subject)
                .ToListAsync(ct);

            if (outOfService.Count > 0)
            {
                var what = outOfService.Count > 1 ? "its vehicle and its driver are"
                    : outOfService[0] == OutOfServiceSubjects.Vehicle ? "its vehicle is" : "its driver is";

                throw new RouteNotRoutableException(
                    $"Route {route.Name} is out of service: {what} out of service. Put it back in service " +
                    "or give the route another vehicle or driver before routing trips on it.",
                    RouteNotRoutableException.OutOfService);
            }

            var day = date.Date;
            var suspended = await context.RouteSuspensions.AsNoTracking()
                .AnyAsync(s => s.VehicleRouteId == route.Id && s.SuspensionStart <= day && s.SuspensionEnd >= day, ct);

            if (suspended)
                throw new RouteNotRoutableException(
                    $"Route {route.Name} is suspended on {day:MM/dd/yyyy}. Trips cannot be routed on it that day.",
                    RouteNotRoutableException.Suspended);
        }

        /// <summary>The open records of a vehicle and a driver.</summary>
        public static IQueryable<OutOfServiceRecord> OpenRecordsFor(RaphaelContext context, int vehicleId, int driverId) =>
            context.OutOfServiceRecords.AsNoTracking()
                .Where(r => r.EndedAtUtc == null
                            && ((r.Subject == OutOfServiceSubjects.Vehicle && r.SubjectId == vehicleId)
                                || (r.Subject == OutOfServiceSubjects.Driver && r.SubjectId == driverId)));
    }
}
