using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Admin;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities.Routing;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Routing
{
    /// <inheritdoc cref="IRoutingService"/>
    public class RoutingService : IRoutingService
    {
        /// <summary>
        /// How long a cached answer serves when no administrator has said otherwise. The real
        /// figure lives in <c>SystemSettings</c> under <c>Routing.CacheRetentionDays</c> and both
        /// the reader here and <c>RouteCachePurgeWorker</c> take it from there, so the two cannot
        /// disagree. A year by business decision — Google's terms describe 30 days, and that
        /// trade-off is the administrator's to make, recorded in the setting, not in code.
        /// </summary>
        public const int DefaultCacheRetentionDays = 365;

        /// <summary>
        /// A dead address is remembered for a week, not a month. Long enough that re-importing
        /// yesterday's CSV costs nothing, short enough that a corrected address is retried within
        /// the same billing period.
        /// </summary>
        public static readonly TimeSpan NotFoundLifetime = TimeSpan.FromDays(7);

        /// <summary>
        /// How many requests may be in flight to Google at once. Not a rate limit — a courtesy
        /// ceiling, so a fifty-leg batch does not open fifty sockets at the same instant.
        /// </summary>
        private static readonly SemaphoreSlim GoogleGate = new(5, 5);

        private const int DefaultBufferPercent = 12;

        private readonly RaphaelContext _context;
        private readonly GoogleRoutesClient _routes;
        private readonly GoogleGeocodingClient _geocoding;
        private readonly ISystemSettingService _settings;
        private readonly IMapsUsageService _usage;
        private readonly IOperationClock _clock;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<RoutingService> _logger;

        public RoutingService(
            RaphaelContext context,
            GoogleRoutesClient routes,
            GoogleGeocodingClient geocoding,
            ISystemSettingService settings,
            IMapsUsageService usage,
            IOperationClock clock,
            ICurrentUserService currentUser,
            ILogger<RoutingService> logger)
        {
            _context = context;
            _routes = routes;
            _geocoding = geocoding;
            _settings = settings;
            _usage = usage;
            _clock = clock;
            _currentUser = currentUser;
            _logger = logger;
        }

        public async Task<RouteLegsResponseDto> GetLegsAsync(
            RouteLegsRequestDto request,
            CancellationToken cancellationToken)
        {
            var mode = await GetTrafficModeAsync(cancellationToken);

            var response = new RouteLegsResponseDto { TrafficMode = mode.ToString() };

            if (request.Legs.Count == 0) return response;

            var bufferPercent = mode == RoutingTrafficMode.MaxSavings
                ? await _settings.GetIntAsync(
                    SystemSettingKeys.RoutingDefaultBufferPercent,
                    DefaultBufferPercent,
                    cancellationToken)
                : 0;

            // The same leg can appear twice in one batch — a route that returns to the same clinic
            // after lunch, or a dispatcher recalculating a there-and-back pair. Group first, and
            // whatever it costs, it costs once.
            var plans = request.Legs.Select(leg => BuildPlan(leg, mode)).ToList();

            var distinctKeys = plans
                .Select(p => p.Key)
                .Distinct()
                .ToList();

            var cached = await LoadCachedAsync(
                distinctKeys,
                mode,
                await GetRetentionAsync(cancellationToken),
                cancellationToken);

            // A map screen wants the road's shape as well. A row cached by the scheduler has no
            // shape in it, so for these keys a hit without one is not a hit.
            var polylineWanted = plans
                .Where(p => p.Leg.IncludePolyline)
                .Select(p => p.Key)
                .ToHashSet();

            var resolved = new Dictionary<LegKey, GoogleLegResult?>();
            var toBuy = new List<LegPlan>();

            foreach (var key in distinctKeys)
            {
                var usable = cached.TryGetValue(key, out var hit)
                    && (!polylineWanted.Contains(key) || !string.IsNullOrEmpty(hit.EncodedPolyline));

                if (usable)
                {
                    resolved[key] = new GoogleLegResult
                    {
                        DurationSeconds = hit!.DurationSeconds,
                        DurationInTrafficSeconds = hit.DurationInTrafficSeconds,
                        DistanceMeters = hit.DistanceMeters,
                        EncodedPolyline = hit.EncodedPolyline
                    };
                }
                else
                {
                    toBuy.Add(plans.First(p => p.Key == key));
                }
            }

            var bought = new HashSet<LegKey>();

            if (toBuy.Count > 0)
            {
                var purchases = await Task.WhenAll(toBuy.Select(plan =>
                    BuyLegAsync(plan, mode, polylineWanted.Contains(plan.Key), cancellationToken)));

                foreach (var (plan, result) in toBuy.Zip(purchases))
                {
                    resolved[plan.Key] = result;

                    if (result is not null) bought.Add(plan.Key);
                }

                await PersistAsync(toBuy, purchases, mode, cancellationToken);
            }

            foreach (var plan in plans)
            {
                resolved.TryGetValue(plan.Key, out var result);

                response.Legs.Add(ToDto(result, plan, mode, bufferPercent, bought.Contains(plan.Key)));
            }

            // Counted per distinct leg, not per answer: a batch that asks for the same clinic
            // twice pays once, and inflating both tallies with the repeat would overstate the
            // spending and the saving by the same amount.
            var sku = mode == RoutingTrafficMode.Precision
                ? MapsSku.RoutesPro
                : MapsSku.RoutesEssentials;

            await _usage.RecordAsync(sku, billed: true, bought.Count, cancellationToken);
            await _usage.RecordAsync(
                sku, billed: false, distinctKeys.Count - bought.Count, cancellationToken);

            return response;
        }

        public async Task<GeocodeResultDto> GeocodeAsync(
            GeocodeRequestDto request,
            CancellationToken cancellationToken)
        {
            var address = !string.IsNullOrWhiteSpace(request.Address)
                ? request.Address!
                : RouteCacheKey.ComposeAddress(request.Street, request.City, request.State, request.Zip);

            var batch = await GeocodeBatchAsync(
                new GeocodeBatchRequestDto { Addresses = new List<string> { address } },
                cancellationToken);

            return batch.Results.FirstOrDefault()
                ?? new GeocodeResultDto
                {
                    Address = address,
                    Status = RoutingContract.Statuses.Unavailable
                };
        }

        public async Task<GeocodeBatchResponseDto> GeocodeBatchAsync(
            GeocodeBatchRequestDto request,
            CancellationToken cancellationToken)
        {
            var response = new GeocodeBatchResponseDto();

            if (request.Addresses.Count == 0) return response;

            // Normalize first, then group. The daily import carries the same dialysis clinic on
            // forty rows, spelled four ways.
            var normalized = request.Addresses
                .Select(a => new { Original = a ?? string.Empty, Key = RouteCacheKey.NormalizeAddress(a) })
                .ToList();

            var distinctKeys = normalized
                .Select(n => n.Key)
                .Where(k => k.Length > 0)
                .Distinct()
                .ToList();

            var now = DateTime.UtcNow;
            var okCutoff = now - await GetRetentionAsync(cancellationToken);
            var notFoundCutoff = now - NotFoundLifetime;

            var cached = await _context.GeocodeCache
                .AsNoTracking()
                .Where(c => distinctKeys.Contains(c.NormalizedAddress))
                .ToListAsync(cancellationToken);

            var usable = cached
                .Where(c => c.Status == GeocodeStatus.Ok
                    ? c.FetchedAtUtc >= okCutoff
                    : c.FetchedAtUtc >= notFoundCutoff)
                .ToDictionary(c => c.NormalizedAddress);

            var toBuy = distinctKeys.Where(k => !usable.ContainsKey(k)).ToList();

            var purchased = new Dictionary<string, GeocodeCacheEntry>();

            foreach (var key in toBuy)
            {
                // The address as first written, not the normalized form: Google reads a real
                // address better than an upper-cased one with the punctuation stripped out.
                var original = normalized.First(n => n.Key == key).Original;

                var entry = await BuyGeocodeAsync(key, original, cancellationToken);

                if (entry is not null) purchased[key] = entry;
            }

            if (purchased.Count > 0)
            {
                await PersistGeocodesAsync(purchased.Values, cancellationToken);
            }

            foreach (var item in normalized)
            {
                if (item.Key.Length == 0)
                {
                    response.Results.Add(new GeocodeResultDto
                    {
                        Address = item.Original,
                        Status = RoutingContract.Statuses.NotFound
                    });

                    continue;
                }

                if (usable.TryGetValue(item.Key, out var hit))
                {
                    response.Results.Add(ToDto(item.Original, hit, RoutingContract.Sources.Cache));
                }
                else if (purchased.TryGetValue(item.Key, out var fresh))
                {
                    response.Results.Add(ToDto(item.Original, fresh, RoutingContract.Sources.Google));
                }
                else
                {
                    response.Results.Add(new GeocodeResultDto
                    {
                        Address = item.Original,
                        Status = RoutingContract.Statuses.Unavailable
                    });
                }
            }

            // The daily import is the reason this split matters: the same dialysis clinic on
            // forty rows is one purchase and thirty-nine hits.
            await _usage.RecordAsync(
                MapsSku.Geocoding, billed: true, purchased.Count, cancellationToken);

            await _usage.RecordAsync(
                MapsSku.Geocoding, billed: false, usable.Count, cancellationToken);

            return response;
        }

        public async Task<ReverseGeocodeResultDto> ReverseGeocodeCityAsync(
            ReverseGeocodeRequestDto request,
            CancellationToken cancellationToken)
        {
            if (request.Latitude == 0 && request.Longitude == 0)
            {
                return new ReverseGeocodeResultDto { Status = RoutingContract.Statuses.NotFound };
            }

            var city = await _geocoding.ReverseGeocodeCityAsync(
                request.Latitude,
                request.Longitude,
                cancellationToken);

            // ⚠️ Always billed: unlike every other path here, reverse geocoding has no cache
            // behind it. It runs when a dispatcher drags a pin, which is rare enough that nobody
            // has paid for a cache yet — but the panel will show it as 100% bought, correctly.
            await _usage.RecordAsync(MapsSku.Geocoding, billed: true, 1, cancellationToken);

            return new ReverseGeocodeResultDto
            {
                City = city,
                Source = RoutingContract.Sources.Google,
                Status = city is null
                    ? RoutingContract.Statuses.NotFound
                    : RoutingContract.Statuses.Ok
            };
        }

        // ---------------------------------------------------------------- legs

        private LegPlan BuildPlan(RouteLegRequestItemDto leg, RoutingTrafficMode mode)
        {
            DateTime? localDeparture = null;

            if (leg.Date.HasValue)
            {
                localDeparture = leg.Date.Value.Date + (leg.DepartureTime ?? TimeSpan.Zero);
            }

            var (bucket, dayType) = RouteCacheKey.BucketFor(mode, localDeparture);

            var key = new LegKey(
                RouteCacheKey.ToE4(leg.OriginLat),
                RouteCacheKey.ToE4(leg.OriginLng),
                RouteCacheKey.ToE4(leg.DestLat),
                RouteCacheKey.ToE4(leg.DestLng),
                bucket,
                dayType);

            return new LegPlan(key, leg, localDeparture);
        }

        private async Task<Dictionary<LegKey, RouteLegCacheEntry>> LoadCachedAsync(
            List<LegKey> keys,
            RoutingTrafficMode mode,
            TimeSpan retention,
            CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow - retention;

            var originLats = keys.Select(k => k.OriginLatE4).Distinct().ToList();
            var destLats = keys.Select(k => k.DestLatE4).Distinct().ToList();

            // Narrowed by latitude on both ends and then matched exactly in memory. A composite
            // key cannot be expressed as a single IN, and one query beats thirty round trips.
            var candidates = await _context.RouteLegCache
                .AsNoTracking()
                .Where(c => c.TrafficMode == mode
                    && c.FetchedAtUtc >= cutoff
                    && originLats.Contains(c.OriginLatE4)
                    && destLats.Contains(c.DestLatE4))
                .ToListAsync(cancellationToken);

            var wanted = keys.ToHashSet();

            return candidates
                .Select(c => new
                {
                    Key = new LegKey(c.OriginLatE4, c.OriginLngE4, c.DestLatE4, c.DestLngE4, c.TimeBucket, c.DayType),
                    Entry = c
                })
                .Where(x => wanted.Contains(x.Key))
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Entry.FetchedAtUtc).First().Entry);
        }

        private async Task<GoogleLegResult?> BuyLegAsync(
            LegPlan plan,
            RoutingTrafficMode mode,
            bool includePolyline,
            CancellationToken cancellationToken)
        {
            await GoogleGate.WaitAsync(cancellationToken);

            try
            {
                return await _routes.ComputeRouteAsync(
                    plan.Leg.OriginLat,
                    plan.Leg.OriginLng,
                    plan.Leg.DestLat,
                    plan.Leg.DestLng,
                    mode,
                    ToUtc(plan.LocalDeparture),
                    includePolyline,
                    cancellationToken);
            }
            finally
            {
                GoogleGate.Release();
            }
        }

        private async Task PersistAsync(
            List<LegPlan> plans,
            GoogleLegResult?[] results,
            RoutingTrafficMode mode,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var seen = new HashSet<LegKey>();

            var wanted = plans
                .Where((plan, i) => results[i] is not null)
                .Select(p => p.Key)
                .ToHashSet();

            // Rows for these legs whatever their age, tracked so they can be updated. Two reasons
            // one may already be here: it expired but the nightly purge has not run yet, or — the
            // common one — the scheduler cached it without a shape and a map has now asked for
            // one. Inserting on top of either would hit the unique index and lose the answer we
            // just paid for.
            var existing = await LoadForWriteAsync(wanted, mode, cancellationToken);

            foreach (var (plan, result) in plans.Zip(results))
            {
                if (result is null) continue;
                if (!seen.Add(plan.Key)) continue;

                if (existing.TryGetValue(plan.Key, out var row))
                {
                    row.DurationSeconds = result.DurationSeconds;
                    row.DurationInTrafficSeconds = result.DurationInTrafficSeconds;
                    row.DistanceMeters = result.DistanceMeters;

                    // Never blank a shape somebody already paid for because this caller did not
                    // ask for one.
                    row.EncodedPolyline = result.EncodedPolyline ?? row.EncodedPolyline;

                    row.FetchedAtUtc = now;

                    continue;
                }

                _context.RouteLegCache.Add(new RouteLegCacheEntry
                {
                    OriginLatE4 = plan.Key.OriginLatE4,
                    OriginLngE4 = plan.Key.OriginLngE4,
                    DestLatE4 = plan.Key.DestLatE4,
                    DestLngE4 = plan.Key.DestLngE4,
                    TimeBucket = plan.Key.TimeBucket,
                    DayType = plan.Key.DayType,
                    TrafficMode = mode,
                    DurationSeconds = result.DurationSeconds,
                    DurationInTrafficSeconds = result.DurationInTrafficSeconds,
                    DistanceMeters = result.DistanceMeters,
                    EncodedPolyline = result.EncodedPolyline,
                    FetchedAtUtc = now
                });
            }

            if (seen.Count == 0) return;

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                // Two dispatchers priced the same leg in the same second and the unique index
                // caught the second one. The answer was already returned and the row is already
                // there: nothing to repair, and nothing worth failing a request over.
                _logger.LogWarning(ex, "A routing cache row was already present; skipping the insert.");

                foreach (var entry in _context.ChangeTracker.Entries<RouteLegCacheEntry>().ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        /// <summary>
        /// The cache rows for these legs, of any age and tracked, so they can be written to.
        /// </summary>
        private async Task<Dictionary<LegKey, RouteLegCacheEntry>> LoadForWriteAsync(
            HashSet<LegKey> keys,
            RoutingTrafficMode mode,
            CancellationToken cancellationToken)
        {
            if (keys.Count == 0) return new Dictionary<LegKey, RouteLegCacheEntry>();

            var originLats = keys.Select(k => k.OriginLatE4).Distinct().ToList();
            var destLats = keys.Select(k => k.DestLatE4).Distinct().ToList();

            var candidates = await _context.RouteLegCache
                .Where(c => c.TrafficMode == mode
                    && originLats.Contains(c.OriginLatE4)
                    && destLats.Contains(c.DestLatE4))
                .ToListAsync(cancellationToken);

            return candidates
                .Select(c => new
                {
                    Key = new LegKey(c.OriginLatE4, c.OriginLngE4, c.DestLatE4, c.DestLngE4, c.TimeBucket, c.DayType),
                    Entry = c
                })
                .Where(x => keys.Contains(x.Key))
                .GroupBy(x => x.Key)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Entry.FetchedAtUtc).First().Entry);
        }

        private RouteLegResultDto ToDto(
            GoogleLegResult? result,
            LegPlan plan,
            RoutingTrafficMode mode,
            int bufferPercent,
            bool wasBought)
        {
            if (result is null)
            {
                return new RouteLegResultDto { Status = RoutingContract.Statuses.Unavailable };
            }

            int planningSeconds;

            // Two independent facts, and they used to be crushed into one field. Whether Google
            // was billed is Source; whether the planning figure is our own estimate is Buffered.
            var buffered = !(mode == RoutingTrafficMode.Precision && result.DurationInTrafficSeconds.HasValue);

            if (buffered)
            {
                // Free-flow plus our own margin. Flagged so no screen presents it as Google's
                // traffic estimate — it is our guess, and it is ours to improve.
                planningSeconds = (int)Math.Round(result.DurationSeconds * (1 + bufferPercent / 100.0));
            }
            else
            {
                planningSeconds = result.DurationInTrafficSeconds.Value;
            }

            var source = wasBought ? RoutingContract.Sources.Google : RoutingContract.Sources.Cache;

            return new RouteLegResultDto
            {
                DurationSeconds = result.DurationSeconds,
                DurationInTrafficSeconds = planningSeconds,
                DistanceMeters = result.DistanceMeters,
                DistanceMiles = RouteCacheKey.ToMiles(result.DistanceMeters),
                EncodedPolyline = plan.Leg.IncludePolyline ? result.EncodedPolyline : null,
                Source = source,
                Buffered = buffered,
                Status = RoutingContract.Statuses.Ok
            };
        }

        // ------------------------------------------------------------ geocoding

        private async Task<GeocodeCacheEntry?> BuyGeocodeAsync(
            string normalizedKey,
            string originalAddress,
            CancellationToken cancellationToken)
        {
            await GoogleGate.WaitAsync(cancellationToken);

            try
            {
                var (result, definitiveNotFound) = await _geocoding.GeocodeAsync(
                    originalAddress,
                    cancellationToken);

                if (result is null && !definitiveNotFound) return null;

                return new GeocodeCacheEntry
                {
                    NormalizedAddress = normalizedKey,
                    Latitude = result?.Latitude,
                    Longitude = result?.Longitude,
                    PlaceId = result?.PlaceId,
                    FormattedAddress = result?.FormattedAddress,
                    Status = result is null ? GeocodeStatus.ZeroResults : GeocodeStatus.Ok,
                    FetchedAtUtc = DateTime.UtcNow
                };
            }
            finally
            {
                GoogleGate.Release();
            }
        }

        private async Task PersistGeocodesAsync(
            IEnumerable<GeocodeCacheEntry> entries,
            CancellationToken cancellationToken)
        {
            foreach (var entry in entries)
            {
                var existing = await _context.GeocodeCache
                    .FirstOrDefaultAsync(c => c.NormalizedAddress == entry.NormalizedAddress, cancellationToken);

                if (existing is null)
                {
                    _context.GeocodeCache.Add(entry);
                }
                else
                {
                    // Expired rather than absent: refresh in place. The unique index leaves no
                    // other option, and a second row for one address is a second answer.
                    existing.Latitude = entry.Latitude;
                    existing.Longitude = entry.Longitude;
                    existing.PlaceId = entry.PlaceId;
                    existing.FormattedAddress = entry.FormattedAddress;
                    existing.Status = entry.Status;
                    existing.FetchedAtUtc = entry.FetchedAtUtc;
                }
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "A geocode cache row was already present; skipping the insert.");
            }
        }

        private static GeocodeResultDto ToDto(string original, GeocodeCacheEntry entry, string source) =>
            new()
            {
                Address = original,
                Latitude = entry.Latitude,
                Longitude = entry.Longitude,
                PlaceId = entry.PlaceId,
                FormattedAddress = entry.FormattedAddress,
                Source = source,
                Status = entry.Status == GeocodeStatus.Ok
                    ? RoutingContract.Statuses.Ok
                    : RoutingContract.Statuses.NotFound
            };

        // ---------------------------------------------------------------- shared

        /// <summary>
        /// How long a cached answer is still served, as the administrators have set it. The purge
        /// worker reads the same setting, so what is readable and what still exists stay aligned.
        /// </summary>
        private async Task<TimeSpan> GetRetentionAsync(CancellationToken cancellationToken)
        {
            var days = await _settings.GetIntAsync(
                SystemSettingKeys.RoutingCacheRetentionDays,
                DefaultCacheRetentionDays,
                cancellationToken);

            if (days < 1) days = 1;

            return TimeSpan.FromDays(days);
        }

        private async Task<RoutingTrafficMode> GetTrafficModeAsync(CancellationToken cancellationToken)
        {
            var raw = await _settings.GetAsync(
                SystemSettingKeys.RoutingTrafficMode,
                nameof(RoutingTrafficMode.MaxSavings),
                cancellationToken);

            return Enum.TryParse<RoutingTrafficMode>(raw, ignoreCase: true, out var mode)
                ? mode
                : RoutingTrafficMode.MaxSavings;
        }

        /// <summary>
        /// Turns the business wall-clock departure into the instant Google needs.
        /// </summary>
        /// <remarks>
        /// 07:30 means 07:30 where the vehicle is. Which instant that is depends on the provider's
        /// timezone, never on the timezone of whatever machine happens to be running this API.
        /// </remarks>
        private DateTime? ToUtc(DateTime? localDeparture)
        {
            if (!localDeparture.HasValue) return null;

            var zone = _clock.ZoneFor(_currentUser.ProviderId);

            var local = DateTime.SpecifyKind(localDeparture.Value, DateTimeKind.Unspecified);

            try
            {
                return TimeZoneInfo.ConvertTimeToUtc(local, zone);
            }
            catch (ArgumentException)
            {
                // The hour does not exist: this is the spring-forward morning and the clock
                // skipped over it. An hour that never happened is not worth refusing a route
                // over — take the next one, which does.
                return TimeZoneInfo.ConvertTimeToUtc(local.AddHours(1), zone);
            }
        }

        private readonly record struct LegKey(
            int OriginLatE4,
            int OriginLngE4,
            int DestLatE4,
            int DestLngE4,
            byte TimeBucket,
            byte DayType);

        private sealed record LegPlan(LegKey Key, RouteLegRequestItemDto Leg, DateTime? LocalDeparture);
    }
}
