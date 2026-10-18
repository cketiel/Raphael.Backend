using System.Threading.Channels;
using Microsoft.Extensions.Options;

namespace Raphael.Api.Services.Places
{
    /// <summary>What the notes worker is asked to do with a trip (RE-054).</summary>
    public enum NotesWork
    {
        /// <summary>Find its places, learn what its file said, compose what the driver reads.</summary>
        Compose = 1,

        /// <summary>The trip is over: learn from it for the trips to come, and leave its comment as it was.</summary>
        Learn = 2,
    }

    /// <summary>Settings of the notes worker (section <c>Notes</c>).</summary>
    public sealed class NotesOptions
    {
        /// <summary>
        /// The switch. Off, nothing is queued and the worker does nothing: imports, saves, routing and
        /// finishing a trip behave exactly as before RE-054. The notes window keeps working.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Trips processed together, sharing one load of places and notes.</summary>
        public int BatchSize { get; set; } = 50;

        /// <summary>Pause between batches, so a large import does not occupy the database.</summary>
        public int PauseBetweenBatchesMs { get; set; } = 100;

        /// <summary>Hour of the nightly pass on the broker's clock (TIME_POLICY): today's and tomorrow's trips.</summary>
        public int NightlyPassHour { get; set; } = 1;

        /// <summary>Trips the queue holds. Past it, new ones are dropped and the nightly pass picks them up.</summary>
        public int QueueCapacity { get; set; } = 100_000;
    }

    /// <summary>
    /// The only door from the rest of the system into the notes (RE-054). Importing, saving and
    /// finishing a trip drop its id here after their own work is saved, and carry on.
    /// </summary>
    /// <remarks>
    /// <see cref="Enqueue(int, NotesWork)"/> never throws, never waits and never touches the database:
    /// it writes to memory or drops the item. A dropped or lost item (a restart empties the queue) is
    /// picked up by the nightly pass. Nothing a caller does depends on it.
    /// </remarks>
    public interface ITripNotesQueue
    {
        void Enqueue(int tripId, NotesWork work);
        void Enqueue(IEnumerable<int> tripIds, NotesWork work);
    }

    public sealed class TripNotesQueue : ITripNotesQueue
    {
        private readonly Channel<(int TripId, NotesWork Work)> _channel;
        private readonly IOptionsMonitor<NotesOptions> _options;
        private readonly ILogger<TripNotesQueue> _logger;
        private long _dropped;

        public TripNotesQueue(IOptionsMonitor<NotesOptions> options, ILogger<TripNotesQueue> logger)
        {
            _options = options;
            _logger = logger;
            _channel = Channel.CreateBounded<(int, NotesWork)>(new BoundedChannelOptions(Math.Max(1_000, options.CurrentValue.QueueCapacity))
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false,
            }, _ => Interlocked.Increment(ref _dropped));
        }

        public ChannelReader<(int TripId, NotesWork Work)> Reader => _channel.Reader;

        /// <summary>Items dropped because the queue was full, since the last time this was read.</summary>
        public long TakeDropped() => Interlocked.Exchange(ref _dropped, 0);

        public void Enqueue(int tripId, NotesWork work)
        {
            try
            {
                if (tripId <= 0 || !_options.CurrentValue.Enabled) return;
                _channel.Writer.TryWrite((tripId, work));
            }
            catch (Exception ex)
            {
                // Nothing may reach the caller: its own work is already done and saved.
                _logger.LogWarning(ex, "Notes queue: could not queue a trip");
            }
        }

        public void Enqueue(IEnumerable<int> tripIds, NotesWork work)
        {
            try
            {
                foreach (var id in tripIds) Enqueue(id, work);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Notes queue: could not queue trips");
            }
        }
    }
}
