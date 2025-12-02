using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public class GpsService : IGpsService
    {
        private readonly RaphaelContext _context; 

        public GpsService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task SaveGpsDataAsync(GpsDataDto gpsDataDto)
        {
            
            var gpsEntity = new GPS
            {
                IdVehicleRoute = gpsDataDto.IdVehicleRoute,
                DateTime = gpsDataDto.DateTime.ToUniversalTime(), // Always save in UTC
                Speed = gpsDataDto.Speed,
                Address = gpsDataDto.Address,
                Latitude = gpsDataDto.Latitude,
                Longitude = gpsDataDto.Longitude,
                Direction = gpsDataDto.Direction
            };

            _context.GPSData.Add(gpsEntity);
            await _context.SaveChangesAsync();
        }
        public async Task<GpsDataDto?> GetLatestGpsDataAsync(int vehicleRouteId)
        {
            var latestGps = await _context.GPSData
                .AsNoTracking() // Improves performance for read-only queries
                .Where(g => g.IdVehicleRoute == vehicleRouteId)
                .OrderByDescending(g => g.DateTime)
                .FirstOrDefaultAsync();

            if (latestGps == null)
            {
                return null; // No data found for this route
            }
          
            return new GpsDataDto
            {
                IdVehicleRoute = latestGps.IdVehicleRoute,
                DateTime = latestGps.DateTime,
                Speed = latestGps.Speed,
                Address = latestGps.Address,
                Latitude = latestGps.Latitude,
                Longitude = latestGps.Longitude,
                Direction = latestGps.Direction
            };
        }

        public async Task<IEnumerable<GpsDataDto>> GetGpsHistoryForReportAsync(int vehicleRouteId, DateTime date)
        {
            // Calculate the start and end of the given day in UTC for a robust query,
            // assuming the dates in the database are stored in UTC.
            var dayStartUtc = date.Date.ToUniversalTime();
            var dayEndUtc = dayStartUtc.AddDays(1);

            var gpsHistory = await _context.GPSData
                .AsNoTracking() // Use AsNoTracking for better performance on read-only queries
                .Where(g => g.IdVehicleRoute == vehicleRouteId &&
                             g.DateTime >= dayStartUtc &&
                             g.DateTime < dayEndUtc)
                .OrderBy(g => g.DateTime) // Order the data chronologically
                .Select(g => new GpsDataDto // Project the entity to a DTO
                {
                    IdVehicleRoute = g.IdVehicleRoute,
                    DateTime = g.DateTime,
                    Speed = g.Speed,
                    Address = g.Address,
                    Latitude = g.Latitude,
                    Longitude = g.Longitude,
                    Direction = g.Direction
                })
                .ToListAsync();

            return gpsHistory;
        }

    }
}
