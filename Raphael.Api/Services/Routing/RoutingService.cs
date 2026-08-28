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
        /// ⚠️ Thirty days, and not a day more. Google's terms allow route and geocoding content to
        /// be cached for at most 30 consecutive days. Reading past this line would breach them
        /// even though the row is still sitting there; deleting it is <c>RouteCachePurgeWorker</c>'s
        /// job, and the two must agree.
        /// </summary>
        public static readonly TimeSpan CacheLifetime = TimeSpan.FromDays(30);

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
        private readonly IOperationClock _clock;
        private readonly ICurrentUserService _currentUser;
        private readonly ILogger<RoutingService> _logger;

        public RoutingService(
            RaphaelContext context,
            GoogleRoutesClient routes,
            GoogleGeocodingClient geocoding,
            ISystemSettingService settings,
            IOperationClock clock,
            ICurrentUserService currentUser,
            ILogger<RoutingService> logger)
        {
            _context = context;
            _routes = routes;
            _geocoding = geocoding;
            _settings = settings;
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

            var cached = await LoadCachedAsync(distinctKeys, mode, cancellationToken);

            var resolved = new Dictionary<LegKey, GoogleLegResult?>();
            var toBuy = new List<LegPlan>();

            foreach (var key in distinctKeys)
            {
                if (cached.TryGetValue(key, out var hit))
                {
                    resolved[key] = new GoogleLegResult
                    {
                        DurationSeconds = hit.DurationSeconds,
                        DurationInTrafficSeconds = hit.DurationInTrafficSeconds,
                        DistanceMeters = hit.DistanceMeters
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
                var purchases = await Task.WhenAll(toBuy.Select(plan => BuyLegAsync(plan, mode, cancellationToken)));

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
            var okCutoff = now - CacheLifetime;
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
            CancellationToken cancellationToken)
        {
            var cutoff = DateTime.UtcNow - CacheLifetime;

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

            foreach (var (plan, result) in plans.Zip(results))
            {
                if (result is null) continue;
                if (!seen.Add(plan.Key)) continue;

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
            string source;

            if (mode == RoutingTrafficMode.Precision && result.DurationInTrafficSeconds.HasValue)
            {
                planningSeconds = result.DurationInTrafficSeconds.Value;
                source = wasBought ? RoutingContract.Sources.Google : RoutingContract.Sources.Cache;
            }
            else
            {
                // Free-flow plus our own margin. Marked Buffered so no screen presents it as
                // Google's traffic estimate — it is our guess, and it is ours to improve.
                planningSeconds = (int)Math.Round(result.DurationSeconds * (1 + bufferPercent / 100.0));
                source = RoutingContract.Sources.Buffered;
            }

            return new RouteLegResultDto
            {
                DurationSeconds = result.DurationSeconds,
                DurationInTrafficSeconds = planningSeconds,
                DistanceMeters = result.DistanceMeters,
                DistanceMiles = RouteCacheKey.ToMiles(result.DistanceMeters),
                Source = source,
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
