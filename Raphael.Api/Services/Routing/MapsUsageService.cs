using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Routing
{
    /// <inheritdoc cref="IMapsUsageService"/>
    public class MapsUsageService : IMapsUsageService
    {
        private readonly RaphaelContext _context;
        private readonly IOperationClock _clock;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<MapsUsageService> _logger;

        public MapsUsageService(
            RaphaelContext context,
            IOperationClock clock,
            ICurrentUserService currentUser,
            ILogger<MapsUsageService> logger)
        {
            _context = context;
            _clock = clock;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task RecordAsync(
            MapsSku sku,
            bool billed,
            int count,
            CancellationToken cancellationToken)
        {
            if (count <= 0) return;

            // The business date, not UTC. An administrator holding a Google invoice next to this
            // panel is thinking in working days, and a route recalculated at eight in the evening
            // belongs to that evening rather than to tomorrow in London.
            var day = _clock.TodayFor(_currentUser.ProviderId).Date;

            try
            {
                // One statement, and the unique index arbitrates. A read-then-write would let two
                // dispatchers finishing a batch in the same millisecond each read 40 and each
                // write 41, quietly losing a request from the tally every time the office is busy.
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $@"
                    UPDATE MapsUsageDaily
                       SET Count = Count + {count}
                     WHERE Day = {day} AND Sku = {(byte)sku} AND Billed = {billed};

                    IF @@ROWCOUNT = 0
                    INSERT INTO MapsUsageDaily (Day, Sku, Billed, Count)
                    VALUES ({day}, {(byte)sku}, {billed}, {count});",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                // ⚠️ Never rethrow. A counter is worth less than the route the dispatcher is
                // waiting for, and a duplicate-key race here means another request inserted the
                // row a microsecond earlier — the next call will find it and add to it.
                _logger.LogWarning(
                    ex, "Could not record Maps usage for {Sku} (billed: {Billed}).", sku, billed);
            }
        }
    }
}
