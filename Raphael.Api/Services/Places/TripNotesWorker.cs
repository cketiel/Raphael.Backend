using System.Diagnostics;
using Microsoft.Extensions.Options;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Places
{
    /// <summary>
    /// The one background process that works the notes (RE-054): the queue, in batches, one at a time,
    /// and a nightly pass over today's and tomorrow's trips for anything the queue lost.
    /// </summary>
    /// <remarks>
    /// Runs apart from every request, with its own database context per batch. A failure here is
    /// logged and the batch is left for the next pass; it never reaches an import, a save, routing or
    /// a driver finishing a trip. One reader: two batches never run at once in an instance.
    /// </remarks>
    public sealed class TripNotesWorker : BackgroundService
    {
        private readonly TripNotesQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly IOptionsMonitor<NotesOptions> _options;
        private readonly ILogger<TripNotesWorker> _logger;

        // No IOperationClock here: it is scoped and this worker lives as long as the API. It is read
        // from a scope each time it is needed, like the notes service.
        public TripNotesWorker(TripNotesQueue queue, IServiceScopeFactory scopes, IOptionsMonitor<NotesOptions> options,
            ILogger<TripNotesWorker> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _options = options;
            _logger = logger;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
            Task.WhenAll(DrainQueueAsync(stoppingToken), NightlyPassAsync(stoppingToken));

        private async Task DrainQueueAsync(CancellationToken stoppingToken)
        {
            var reader = _queue.Reader;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    if (!await reader.WaitToReadAsync(stoppingToken)) return;

                    var size = Math.Clamp(_options.CurrentValue.BatchSize, 1, 500);
                    var batch = new List<(int TripId, NotesWork Work)>(size);
                    while (batch.Count < size && reader.TryRead(out var item)) batch.Add(item);

                    if (!_options.CurrentValue.Enabled) continue;

                    // The same trip queued twice in a batch is worked once; Compose wins over Learn.
                    var compose = batch.Where(b => b.Work == NotesWork.Compose).Select(b => b.TripId).Distinct().ToList();
                    var learn = batch.Where(b => b.Work == NotesWork.Learn).Select(b => b.TripId).Distinct().Except(compose).ToList();
                    if (compose.Count > 0) await RunAsync(compose, NotesWork.Compose, stoppingToken);
                    if (learn.Count > 0) await RunAsync(learn, NotesWork.Learn, stoppingToken);

                    var dropped = _queue.TakeDropped();
                    if (dropped > 0)
                        _logger.LogWarning("Notes queue was full: {Dropped} trip(s) dropped, left for the nightly pass", dropped);

                    await Task.Delay(Math.Max(0, _options.CurrentValue.PauseBetweenBatchesMs), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notes worker: a batch failed; it is left for the nightly pass");
                }
            }
        }

        private async Task RunAsync(List<int> tripIds, NotesWork work, CancellationToken stoppingToken)
        {
            var watch = Stopwatch.StartNew();
            using var scope = _scopes.CreateScope();
            var notes = scope.ServiceProvider.GetRequiredService<ITripNotesService>();
            var failed = await notes.ProcessTripsAsync(tripIds, work, stoppingToken);
            _logger.LogInformation("Notes worker: {Work} {Count} trip(s) in {ElapsedMs} ms, {Failed} failed",
                work, tripIds.Count, watch.ElapsedMilliseconds, failed);
        }

        /// <summary>Once a night, at <see cref="NotesOptions.NightlyPassHour"/> on the broker's clock.</summary>
        private async Task NightlyPassAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    DateTime now;
                    using (var clockScope = _scopes.CreateScope())
                        now = clockScope.ServiceProvider.GetRequiredService<IOperationClock>().NowFor(null);
                    var next = now.Date.AddHours(Math.Clamp(_options.CurrentValue.NightlyPassHour, 0, 23));
                    if (next <= now) next = next.AddDays(1);
                    await Task.Delay(next - now, stoppingToken);

                    if (!_options.CurrentValue.Enabled) continue;

                    using var scope = _scopes.CreateScope();
                    var notes = scope.ServiceProvider.GetRequiredService<ITripNotesService>();
                    var ids = await notes.TripsForNightlyPassAsync(stoppingToken);
                    _queue.Enqueue(ids, NotesWork.Compose);
                    _logger.LogInformation("Notes nightly pass: {Count} trip(s) of today and tomorrow queued", ids.Count);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Notes nightly pass failed; it runs again tomorrow");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
            }
        }
    }
}
