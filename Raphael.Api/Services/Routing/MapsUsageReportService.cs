using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities.Routing;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Routing
{
    /// <inheritdoc cref="IMapsUsageReportService"/>
    public class MapsUsageReportService : IMapsUsageReportService
    {
        private readonly RaphaelContext _context;
        private readonly IOperationClock _clock;
        private readonly ICurrentUserService _currentUser;

        public MapsUsageReportService(
            RaphaelContext context,
            IOperationClock clock,
            ICurrentUserService currentUser)
        {
            _context = context;
            _clock = clock;
            _currentUser = currentUser;
        }

        public async Task<MapsUsageSummaryDto> GetSummaryAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            from = from.Date;
            to = to.Date;

            var rows = await _context.MapsUsageDaily
                .AsNoTracking()
                .Where(u => u.Day >= from && u.Day <= to)
                .ToListAsync(cancellationToken);

            var tiers = await _context.MapsPricingTiers
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var today = _clock.TodayFor(_currentUser.ProviderId).Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);

            // What this calendar month has already spent, for two different jobs: telling the
            // administrator how much free allowance is left, and pricing the period's requests at
            // the band they would really have fallen into rather than from zero.
            var thisMonth = await _context.MapsUsageDaily
                .AsNoTracking()
                .Where(u => u.Day >= monthStart && u.Day <= today && u.Billed)
                .GroupBy(u => u.Sku)
                .Select(g => new { Sku = g.Key, Count = g.Sum(x => (long)x.Count) })
                .ToDictionaryAsync(x => x.Sku, x => x.Count, cancellationToken);

            var summary = new MapsUsageSummaryDto { From = from, To = to };

            foreach (MapsSku sku in Enum.GetValues<MapsSku>())
            {
                var billed = rows.Where(r => r.Sku == sku && r.Billed).Sum(r => (long)r.Count);
                var cached = rows.Where(r => r.Sku == sku && !r.Billed).Sum(r => (long)r.Count);

                if (billed == 0 && cached == 0) continue;

                var freeCap = MapsPricingCalculator.FreeCap(tiers, sku);
                var usedThisMonth = thisMonth.TryGetValue(sku, out var used) ? used : 0;

                var cost = MapsPricingCalculator.Cost(tiers, sku, billed);

                // The saving is what those requests would have cost *on top of* the ones we made:
                // they would have landed in a higher band, not started again from the free tier.
                // Pricing them from zero would flatter the figure, and this panel is meant to be
                // shown to somebody who will check it.
                var avoided = MapsPricingCalculator.Cost(tiers, sku, cached, billed);

                summary.BySku.Add(new MapsSkuUsageDto
                {
                    Sku = sku.ToString(),
                    DisplayName = MapsPricingCalculator.DisplayName(sku),
                    Billed = billed,
                    Cached = cached,
                    ReportedByClient = MapsPricingCalculator.IsReportedByClient(sku),
                    EstimatedCost = cost,
                    AvoidedCost = avoided,
                    FreeCapPerMonth = freeCap,
                    FreeRemainingThisMonth = Math.Max(0, freeCap - usedThisMonth)
                });

                summary.TotalBilled += billed;
                summary.TotalCached += cached;
                summary.EstimatedCost += cost;
                summary.AvoidedCost += avoided;
            }

            var total = summary.TotalBilled + summary.TotalCached;

            summary.CacheHitRate = total == 0 ? 0 : (double)summary.TotalCached / total;
            summary.ProjectedMonthCost = await ProjectMonthAsync(from, to, tiers, cancellationToken);

            return summary;
        }

        /// <summary>
        /// Where this month's bill is heading at the rate it has run so far.
        /// </summary>
        /// <remarks>
        /// Only offered while the month is still open. Projecting a period that has already closed
        /// is arithmetic dressed up as a forecast, and an administrator setting a Cloud Console
        /// quota deserves the difference.
        /// </remarks>
        private async Task<decimal?> ProjectMonthAsync(
            DateTime from,
            DateTime to,
            IReadOnlyList<MapsPricingTier> tiers,
            CancellationToken cancellationToken)
        {
            var today = _clock.TodayFor(_currentUser.ProviderId).Date;
            var monthStart = new DateTime(today.Year, today.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            if (to < monthStart || from > monthEnd) return null;

            var rows = await _context.MapsUsageDaily
                .AsNoTracking()
                .Where(u => u.Day >= monthStart && u.Day <= today && u.Billed)
                .GroupBy(u => u.Sku)
                .Select(g => new { Sku = g.Key, Count = g.Sum(x => (long)x.Count) })
                .ToListAsync(cancellationToken);

            if (rows.Count == 0) return 0m;

            var elapsed = (today - monthStart).Days + 1;
            var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);

            var projected = 0m;

            foreach (var row in rows)
            {
                var perDay = (double)row.Count / elapsed;
                var wholeMonth = (long)Math.Round(perDay * daysInMonth);

                projected += MapsPricingCalculator.Cost(tiers, row.Sku, wholeMonth);
            }

            return projected;
        }

        public async Task<IReadOnlyList<MapsUsagePointDto>> GetDailyAsync(
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken)
        {
            from = from.Date;
            to = to.Date;

            var rows = await _context.MapsUsageDaily
                .AsNoTracking()
                .Where(u => u.Day >= from && u.Day <= to)
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(r => new { r.Day, r.Sku })
                .Select(g => new MapsUsagePointDto
                {
                    Day = g.Key.Day,
                    Sku = g.Key.Sku.ToString(),
                    Billed = g.Where(x => x.Billed).Sum(x => (long)x.Count),
                    Cached = g.Where(x => !x.Billed).Sum(x => (long)x.Count)
                })
                .OrderBy(p => p.Day)
                .ThenBy(p => p.Sku)
                .ToList();
        }

        public async Task<MapsUsageTotalsDto> GetTotalsAsync(CancellationToken cancellationToken)
        {
            var billed = await _context.MapsUsageDaily
                .AsNoTracking()
                .Where(u => u.Billed)
                .SumAsync(u => (long)u.Count, cancellationToken);

            var cached = await _context.MapsUsageDaily
                .AsNoTracking()
                .Where(u => !u.Billed)
                .SumAsync(u => (long)u.Count, cancellationToken);

            var any = await _context.MapsUsageDaily.AsNoTracking().AnyAsync(cancellationToken);

            var total = billed + cached;

            return new MapsUsageTotalsDto
            {
                Billed = billed,
                Cached = cached,
                CacheHitRate = total == 0 ? 0 : (double)cached / total,
                FirstDay = any
                    ? await _context.MapsUsageDaily.AsNoTracking().MinAsync(u => u.Day, cancellationToken)
                    : null,
                LastDay = any
                    ? await _context.MapsUsageDaily.AsNoTracking().MaxAsync(u => u.Day, cancellationToken)
                    : null
            };
        }

        public async Task<IReadOnlyList<MapsPricingTierDto>> GetPricingAsync(
            CancellationToken cancellationToken)
        {
            var tiers = await _context.MapsPricingTiers
                .AsNoTracking()
                .OrderBy(t => t.Sku)
                .ThenBy(t => t.FromRequest)
                .ToListAsync(cancellationToken);

            return tiers.Select(t => new MapsPricingTierDto
            {
                Id = t.Id,
                Sku = t.Sku.ToString(),
                DisplayName = MapsPricingCalculator.DisplayName(t.Sku),
                FreeCapPerMonth = t.FreeCapPerMonth,
                FromRequest = t.FromRequest,
                ToRequest = t.ToRequest,
                PricePerThousand = t.PricePerThousand
            }).ToList();
        }
    }
}
