using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raphael.Shared.DbContexts;

namespace Raphael.Shared.Time;

/// <summary>
/// Resolves the operating timezone from the provider carrying out the trip.
/// </summary>
/// <remarks>
/// The chain is deliberately short and has no step that consults the host:
/// <c>Trip.ProviderId</c> → that provider's <c>TimeZoneId</c> → the configured default.
/// A trip with no provider is one the broker runs itself and takes the broker's zone.
/// </remarks>
public sealed class OperationClock : IOperationClock
{
    /// <summary>
    /// How long the provider timezones are held before being read again.
    /// </summary>
    /// <remarks>
    /// Providers are edited a handful of times a year, and a correction made in the admin
    /// screen invalidates this straight away, so the window only matters if a row is changed
    /// outside the application.
    /// </remarks>
    private static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(10);

    private const string CacheKey = "operation-time-zones";

    private readonly RaphaelContext _context;

    private readonly IMemoryCache _cache;

    private readonly OperationTimeOptions _options;

    private readonly ILogger<OperationClock> _logger;

    public OperationClock(
        RaphaelContext context,
        IMemoryCache cache,
        IOptions<OperationTimeOptions> options,
        ILogger<OperationClock> logger)
    {
        _context = context;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public TimeZoneInfo ZoneFor(int? providerId)
    {
        // No provider means the broker runs this trip itself.
        var id = providerId ?? _options.BrokerProviderId;

        var declared = ZoneIds().GetValueOrDefault(id);

        if (!string.IsNullOrWhiteSpace(declared) &&
            TryFind(declared, out var zone))
        {
            return zone;
        }

        if (!string.IsNullOrWhiteSpace(declared))
        {
            // Declared but unusable. Worth saying out loud: somebody typed it, and it is
            // silently not doing what they think it is doing.
            _logger.LogWarning(
                "Provider {ProviderId} declares timezone '{TimeZoneId}', which this host does " +
                "not recognise. Falling back to the configured default.",
                id,
                declared);
        }

        return Default();
    }

    public DateTime NowFor(int? providerId) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZoneFor(providerId));

    public DateTime TodayFor(int? providerId) =>
        NowFor(providerId).Date;

    public TimeSpan TimeOfDayFor(int? providerId) =>
        NowFor(providerId).TimeOfDay;

    public DateTime ToOperation(DateTime utc, int? providerId) =>
        TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc),
            ZoneFor(providerId));

    public void InvalidateZones() =>
        _cache.Remove(CacheKey);

    /// <summary>
    /// Every provider's declared timezone, in one query, cached.
    /// </summary>
    /// <remarks>
    /// All of them rather than one at a time: there are a handful of rows, and a single
    /// cached dictionary is cheaper than a query per trip on a busy dispatch screen.
    /// </remarks>
    private Dictionary<int, string?> ZoneIds()
    {
        return _cache.GetOrCreate(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;

            return _context.Providers
                .AsNoTracking()
                .Select(provider => new { provider.Id, provider.TimeZoneId })
                .ToDictionary(row => row.Id, row => row.TimeZoneId);
        }) ?? [];
    }

    private TimeZoneInfo Default() =>
        Resolve(_options.DefaultTimeZone);

    /// <summary>
    /// Resolves a configured timezone, or refuses.
    /// </summary>
    /// <remarks>
    /// ⚠️ Throws rather than falling back to the machine's own zone. A misconfiguration that
    /// quietly borrows the host's clock is the exact defect this class exists to remove, and
    /// it would be invisible until somebody noticed trips were hours out. Called once at
    /// startup so a bad value stops the deployment instead of corrupting a shift.
    /// </remarks>
    public static TimeZoneInfo Resolve(string timeZoneId)
    {
        if (TryFind(timeZoneId, out var zone))
            return zone;

        throw new InvalidOperationException(
            $"'{timeZoneId}' is not a timezone this host recognises. Set " +
            $"{OperationTimeOptions.SectionName}:{nameof(OperationTimeOptions.DefaultTimeZone)} " +
            "to an IANA identifier such as 'America/New_York'. The API will not fall back to " +
            "the server's own timezone: that is what this setting exists to prevent.");
    }

    private static bool TryFind(string? timeZoneId, out TimeZoneInfo zone)
    {
        zone = TimeZoneInfo.Utc;

        if (string.IsNullOrWhiteSpace(timeZoneId))
            return false;

        try
        {
            // .NET 8 accepts IANA identifiers on Windows and Windows identifiers on Linux,
            // so the same configuration works either side of a move.
            zone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (Exception ex) when (
            ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return false;
        }
    }
}
