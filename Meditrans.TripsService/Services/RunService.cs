using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Meditrans.TripsService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.TripsService.Services
{
    public class RunService : IRunService
    {
        private readonly MediTransContext _context;

        public RunService(MediTransContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<VehicleRoute>> GetAllAsync()
        {
            return await _context.VehicleRoutes
                .Include(vr => vr.Vehicle)
                .Include(vr => vr.Driver)
                .ToListAsync();
        }

        public async Task<VehicleRoute?> GetByIdAsync(int id)
        {
            return await _context.VehicleRoutes
                .Include(vr => vr.Vehicle)
                .Include(vr => vr.Driver)
                .FirstOrDefaultAsync(vr => vr.Id == id);
        }

        public async Task<VehicleRoute> CreateAsync(RunDto dto)
        {
            var route = new VehicleRoute
            {
                Name = dto.Name,
                Description = dto.Description,
                DriverId = dto.DriverId,
                VehicleId = dto.VehicleId,
                Garage = dto.Garage
            };

            _context.VehicleRoutes.Add(route);
            await _context.SaveChangesAsync();
            return route;
        }

        public async Task<bool> UpdateAsync(int id, RunDto dto)
        {
            var route = await _context.VehicleRoutes.FindAsync(id);
            if (route == null) return false;

            route.Name = dto.Name;
            route.Description = dto.Description;
            route.DriverId = dto.DriverId;
            route.VehicleId = dto.VehicleId;
            route.Garage = dto.Garage;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var route = await _context.VehicleRoutes.FindAsync(id);
            if (route == null) return false;

            _context.VehicleRoutes.Remove(route);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
