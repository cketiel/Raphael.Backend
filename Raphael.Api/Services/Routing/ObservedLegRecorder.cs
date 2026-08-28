using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Routing;
using Raphael.Shared.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Records how long a leg really took, every time a driver arrives somewhere.
    /// </summary>
    /// <remarks>
    /// The fleet drives the same roads at the same hours every week and, until now, threw away
    /// what it learned each time. These rows are the ecosystem's own answer to "how long does this
    /// take at eight in the morning" — the question the automatic router will be built around, and
    /// the one that lets a free-flow duration be buffered honestly instead of guessed at.
    /// </remarks>
    public interface IObservedLegRecorder
    {
        /// <summary>
        /// Records the drive that ended at <paramref name="schedule"/>, if it can be measured.
        /// </summary>
        /// <remarks>
        /// Never throws and never fails the caller: this is bookkeeping alongside a driver
        /// confirming an event, and no measurement is worth losing an arrival over.
        /// </remarks>
        Task RecordArrivalAsync(Schedule schedule, CancellationToken cancellationToken = default);
    }

    /// <inheritdoc cref="IObservedLegRecorder"/>
    public class ObservedLegRecorder : IObservedLegRecorder
    {
        /// <summary>
        /// Under a minute is not a drive — it is two stops at the same address, or a driver
        /// catching up on the paperwork of both at once.
        /// </summary>
        private static readonly TimeSpan MinimumLeg = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Over three hours is not a leg in this operation: it is a driver who forgot to press
        /// Arrive until after lunch. Letting it in would poison the average for that hour.
        /// </summary>
        private static readonly TimeSpan MaximumLeg = TimeSpan.FromHours(3);

        private readonly RaphaelContext _context;
        private readonly ILogger<ObservedLegRecorder> _logger;

        public ObservedLegRecorder(RaphaelContext context, ILogger<ObservedLegRecorder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RecordArrivalAsync(
            Schedule schedule,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await RecordCoreAsync(schedule, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not record an observed leg time for schedule {ScheduleId}.", schedule.Id);
            }
        }

        private async Task RecordCoreAsync(Schedule schedule, CancellationToken cancellationToken)
        {
            if (!schedule.ActualArriveTime.HasValue) return;
            if (schedule.Sequence is null or 0) return;
            if (schedule.ScheduleLatitude == 0 && schedule.ScheduleLongitude == 0) return;

            // The stop this vehicle left to get here: the previous one in the sequence that the
            // driver actually finished. Anything else — a cancelled stop nobody drove to, a stop
            // still pending — would measure a drive that did not happen.
            var previous = await _context.Schedules
                .AsNoTracking()
                .Where(s => s.VehicleRouteId == schedule.VehicleRouteId
                    && s.Date == schedule.Date
                    && s.Sequence < schedule.Sequence
                    && s.ActualPerformTime != null)
                .OrderByDescending(s => s.Sequence)
                .FirstOrDefaultAsync(cancellationToken);

            if (previous?.ActualPerformTime is null) return;
            if (previous.ScheduleLatitude == 0 && previous.ScheduleLongitude == 0) return;

            var departure = previous.ActualPerformTime.Value;
            var arrival = schedule.ActualArriveTime.Value;

            var elapsed = arrival - departure;

            // Both are times of day on the same service date. A leg that appears to take negative
            // time is one that crossed midnight, and a night route is not what this table is for.
            if (elapsed < MinimumLeg || elapsed > MaximumLeg) return;

            var serviceDate = schedule.Date?.Date ?? DateTime.UtcNow.Date;

            var (bucket, dayType) = RouteCacheKey.ObservedBucketFor(serviceDate + departure);

            _context.ObservedLegTimes.Add(new ObservedLegTime
            {
                OriginLatE4 = RouteCacheKey.ToE4(previous.ScheduleLatitude),
                OriginLngE4 = RouteCacheKey.ToE4(previous.ScheduleLongitude),
                DestLatE4 = RouteCacheKey.ToE4(schedule.ScheduleLatitude),
                DestLngE4 = RouteCacheKey.ToE4(schedule.ScheduleLongitude),
                TimeBucket = bucket,
                DayType = dayType,
                DurationSeconds = (int)elapsed.TotalSeconds,
                DistanceMeters = schedule.DistanceToPoint.HasValue
                    ? (int)Math.Round(schedule.DistanceToPoint.Value * 1609.344)
                    : null,
                Source = ObservedLegSource.SchedulePerformed,
                VehicleRouteId = schedule.VehicleRouteId,
                ScheduleId = schedule.Id,
                ObservedAtUtc = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
