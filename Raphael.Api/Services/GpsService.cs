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

    }
}
