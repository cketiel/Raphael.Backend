using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities;
using System.Globalization;

namespace Raphael.Api.Services.Admin
{
    /// <inheritdoc cref="ISystemSettingService"/>
    public class SystemSettingService : ISystemSettingService
    {
        /// <summary>
        /// Long enough that a screen pricing a whole route reads the database once, short enough
        /// that an administrator who flips the mode sees it take effect while still looking at it.
        /// </summary>
        private static readonly TimeSpan CacheFor = TimeSpan.FromSeconds(60);

        private const string CachePrefix = "systemsetting:";

        private readonly RaphaelContext _context;
        private readonly IMemoryCache _cache;

        public SystemSettingService(RaphaelContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<string> GetAsync(
            string key,
            string fallback,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue<string>(CachePrefix + key, out var cached) && cached is not null)
            {
                return cached;
            }

            var value = await _context.SystemSettings
                .AsNoTracking()
                .Where(s => s.Key == key)
                .Select(s => s.Value)
                .FirstOrDefaultAsync(cancellationToken);

            // The fallback is cached too. A key that is not in the table yet is the normal state
            // before an administrator has ever touched it, and re-asking every time would put a
            // query on the hot path forever.
            var effective = string.IsNullOrWhiteSpace(value) ? fallback : value;

            _cache.Set(CachePrefix + key, effective, CacheFor);

            return effective;
        }

        public async Task<int> GetIntAsync(
            string key,
            int fallback,
            CancellationToken cancellationToken = default)
        {
            var raw = await GetAsync(key, fallback.ToString(CultureInfo.InvariantCulture), cancellationToken);

            return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public async Task<IReadOnlyList<SystemSettingDto>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return await _context.SystemSettings
                .AsNoTracking()
                .OrderBy(s => s.Key)
                .Select(s => new SystemSettingDto
                {
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Description,
                    UpdatedAtUtc = s.UpdatedAtUtc,
                    UpdatedBy = s.UpdatedBy
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<SystemSettingDto?> GetOneAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            return await _context.SystemSettings
                .AsNoTracking()
                .Where(s => s.Key == key)
                .Select(s => new SystemSettingDto
                {
                    Key = s.Key,
                    Value = s.Value,
                    Description = s.Description,
                    UpdatedAtUtc = s.UpdatedAtUtc,
                    UpdatedBy = s.UpdatedBy
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<SystemSettingDto> SetAsync(
            string key,
            string value,
            string? updatedBy,
            CancellationToken cancellationToken = default)
        {
            var setting = await _context.SystemSettings
                .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);

            if (setting is null)
            {
                setting = new SystemSetting { Key = key };
                _context.SystemSettings.Add(setting);
            }

            setting.Value = value;
            setting.UpdatedAtUtc = DateTime.UtcNow;
            setting.UpdatedBy = updatedBy;

            await _context.SaveChangesAsync(cancellationToken);

            _cache.Remove(CachePrefix + key);

            return new SystemSettingDto
            {
                Key = setting.Key,
                Value = setting.Value,
                Description = setting.Description,
                UpdatedAtUtc = setting.UpdatedAtUtc,
                UpdatedBy = setting.UpdatedBy
            };
        }
    }
}
