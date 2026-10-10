using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Realtime;
using Raphael.Shared.Entities;
using Raphael.Shared.Routing;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// Turns one vehicle position into what a clinic watching one of its trips needs: the phase,
    /// the miles still to go and the route's current ETAs.
    /// </summary>
    /// <remarks>
    /// Computed when the position arrives and sent, never stored: the GPS table does not change.
    /// Nothing here calls Google (MAPS_POLICY): the road is the shape the routing cache already
    /// holds for the leg, if anyone bought it. The whole flow is written down in BACKLOG §17
    /// <c>live-eta</c>, and the driver's "Start trip" button only has to write the pickup ETA for
    /// it to show here with the next fix.
    /// </remarks>
    public interface ITripProgress
    {
        /// <summary>
        /// The message for each trip, read with the query filter off: the caller decides whose
        /// trips these are (the broadcaster, the route's under-way trips; the hub, the clinic's own).
        /// </summary>
        Task<IReadOnlyList<TripVehiclePositionMessage>> ForTripsAsync(
            IReadOnlyCollection<int> tripIds, double latitude, double longitude, double speed, string? direction, DateTime atUtc);
    }

    public class TripProgress : ITripProgress
    {
        /// <summary>A leg's shape does not change once bought; reading it once per trip is enough.</summary>
        private static readonly TimeSpan ShapeCacheTime = TimeSpan.FromMinutes(10);

        private readonly RaphaelContext _context;
        private readonly IMemoryCache _cache;

        public TripProgress(RaphaelContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IReadOnlyList<TripVehiclePositionMessage>> ForTripsAsync(
            IReadOnlyCollection<int> tripIds, double latitude, double longitude, double speed, string? direction, DateTime atUtc)
        {
            if (tripIds.Count == 0) return Array.Empty<TripVehiclePositionMessage>();

            var trips = await _context.Trips
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(t => tripIds.Contains(t.Id))
                .Select(t => new
                {
                    t.Id,
                    t.Status,
                    t.PickupLatitude,
                    t.PickupLongitude,
                    t.DropoffLatitude,
                    t.DropoffLongitude
                })
                .ToListAsync();

            var stops = await _context.Schedules
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(s => s.TripId != null && tripIds.Contains(s.TripId.Value))
                .Select(s => new { TripId = s.TripId!.Value, s.EventType, s.ETATime, s.ActualPerformTime, s.Performed })
                .ToListAsync();

            var messages = new List<TripVehiclePositionMessage>(trips.Count);

            foreach (var trip in trips)
            {
                var pickup = stops.FirstOrDefault(s => s.TripId == trip.Id && s.EventType == ScheduleEventType.Pickup);
                var dropoff = stops.FirstOrDefault(s => s.TripId == trip.Id && s.EventType == ScheduleEventType.Dropoff);

                // On board once the pickup is performed, or once the status says so: either is
                // enough, because the driver's app writes them in two different requests.
                var onBoard = trip.Status == TripStatus.InProgress
                              || pickup?.ActualPerformTime != null
                              || pickup?.Performed == true;

                double meters;
                if (onBoard)
                {
                    var shape = await ShapeAsync(trip.PickupLatitude, trip.PickupLongitude, trip.DropoffLatitude, trip.DropoffLongitude);
                    meters = RemainingDistance.MetersToGo(latitude, longitude, trip.DropoffLatitude, trip.DropoffLongitude, shape);
                }
                else
                {
                    // Heading to the pickup from wherever the previous stop was: no shape of that
                    // leg is known for this trip, so the straight line with the road factor.
                    meters = RemainingDistance.MetersToGo(latitude, longitude, trip.PickupLatitude, trip.PickupLongitude, null);
                }

                messages.Add(new TripVehiclePositionMessage
                {
                    TripId = trip.Id,
                    Latitude = latitude,
                    Longitude = longitude,
                    Speed = speed,
                    Direction = direction,
                    AtUtc = atUtc,
                    Phase = onBoard ? TripPhase.Dropoff : TripPhase.Pickup,
                    RemainingMiles = Math.Round(meters / RemainingDistance.MetersPerMile, 1),
                    PickupEta = pickup?.ETATime,
                    DropoffEta = dropoff?.ETATime
                });
            }

            return messages;
        }

        /// <summary>
        /// The road from pickup to drop-off, if the routing cache holds a shape for it: any hour,
        /// any traffic mode, the newest one. Null when nobody has drawn that leg yet.
        /// </summary>
        private Task<string?> ShapeAsync(double fromLat, double fromLng, double toLat, double toLng)
        {
            int oLat = RouteCacheKey.ToE4(fromLat), oLng = RouteCacheKey.ToE4(fromLng);
            int dLat = RouteCacheKey.ToE4(toLat), dLng = RouteCacheKey.ToE4(toLng);
            var key = $"leg-shape:{oLat},{oLng}:{dLat},{dLng}";

            return _cache.GetOrCreateAsync(key, async entry =>
            {
                var shape = await _context.RouteLegCache
                    .AsNoTracking()
                    .Where(c => c.OriginLatE4 == oLat && c.OriginLngE4 == oLng
                                && c.DestLatE4 == dLat && c.DestLngE4 == dLng
                                && c.EncodedPolyline != null)
                    .OrderByDescending(c => c.FetchedAtUtc)
                    .Select(c => c.EncodedPolyline)
                    .FirstOrDefaultAsync();

                // A miss is cached for less time: the tracking page may buy the shape any minute.
                entry.AbsoluteExpirationRelativeToNow = shape == null ? TimeSpan.FromMinutes(1) : ShapeCacheTime;
                return shape;
            });
        }
    }
}
