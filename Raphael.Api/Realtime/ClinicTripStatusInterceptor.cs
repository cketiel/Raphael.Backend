using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Raphael.Shared.DTOs.Realtime;
using Raphael.Shared.Entities;

namespace Raphael.Api.Realtime
{
    /// <summary>
    /// Tells a clinic's open trip list whenever one of its trips changes status.
    /// </summary>
    /// <remarks>
    /// A trip's status is written in some twenty-five places across four services. Hooking each
    /// one would leave the next one to be written without the message; watching the save catches
    /// them all. The message goes out only after the save succeeds, and a failure to send never
    /// undoes it: a list that misses one is corrected the next time it loads.
    /// </remarks>
    public sealed class ClinicTripStatusInterceptor : SaveChangesInterceptor
    {
        private readonly IHubContext<DispatchHub, IDispatchClient> _hub;
        private readonly ILogger<ClinicTripStatusInterceptor> _logger;

        // The trips each context is about to save, kept until that same save finishes.
        private readonly ConditionalWeakTable<DbContext, List<Trip>> _pending = new();

        public ClinicTripStatusInterceptor(
            IHubContext<DispatchHub, IDispatchClient> hub,
            ILogger<ClinicTripStatusInterceptor> logger)
        {
            _hub = hub;
            _logger = logger;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Collect(eventData.Context);
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Collect(eventData.Context);
            return ValueTask.FromResult(result);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            _ = PublishAsync(eventData.Context);
            return result;
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            await PublishAsync(eventData.Context);
            return result;
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            if (eventData.Context is not null) _pending.Remove(eventData.Context);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            if (eventData.Context is not null) _pending.Remove(eventData.Context);
            return Task.CompletedTask;
        }

        private void Collect(DbContext? context)
        {
            if (context is null) return;

            var changed = context.ChangeTracker.Entries<Trip>()
                .Where(e => e.Entity.IntegratorId != null
                            && (e.State == EntityState.Added
                                || (e.State == EntityState.Modified
                                    && (e.Property(t => t.Status).IsModified || e.Property(t => t.IsCancelled).IsModified))))
                .Select(e => e.Entity)
                .ToList();

            if (changed.Count == 0) return;

            _pending.AddOrUpdate(context, changed);
        }

        private async Task PublishAsync(DbContext? context)
        {
            if (context is null || !_pending.TryGetValue(context, out var trips)) return;

            _pending.Remove(context);

            foreach (var trip in trips)
            {
                try
                {
                    await _hub.Clients
                        .Group(DispatchGroups.Clinic(trip.IntegratorId!.Value))
                        .TripStatusChanged(new TripStatusChangedMessage
                        {
                            TripId = trip.Id,
                            Status = trip.Status,
                            IsCancelled = trip.IsCancelled,
                            Date = trip.Date.Date
                        });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not tell clinic {IntegratorId} that trip {TripId} changed status.",
                        trip.IntegratorId, trip.Id);
                }
            }
        }
    }
}
