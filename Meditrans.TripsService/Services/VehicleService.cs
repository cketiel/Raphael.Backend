using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.TripsService.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Raphael.TripsService.Services
{
    public class VehicleService : IVehicleService
    {
        private readonly RaphaelContext _context;

        public VehicleService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<Vehicle>> GetAllAsync()
        {
            return await _context.Vehicles
                .Include(v => v.VehicleGroup)
                .Include(v => v.CapacityDetailType)
                .Include(v => v.VehicleType)
                .ToListAsync();
        }

        public async Task<Vehicle?> GetByIdAsync(int id)
        {
            return await _context.Vehicles
                .Include(v => v.VehicleGroup)
                .Include(v => v.CapacityDetailType)
                .Include(v => v.VehicleType)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Vehicle> CreateAsync(VehicleDto dto)
        {
            var vehicle = new Vehicle
            {
                Name = dto.Name,
                VIN = dto.VIN,
                Make = dto.Make,
                Model = dto.Model,
                Color = dto.Color,
                Year = dto.Year,
                Plate = dto.Plate,
                ExpirationDate = dto.ExpirationDate,
                IsInactive = dto.IsInactive,
                GroupId = dto.GroupId,
                CapacityDetailTypeId = dto.CapacityDetailTypeId,
                VehicleTypeId = dto.VehicleTypeId
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<Vehicle?> UpdateAsync(int id, VehicleDto dto)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return null;

            vehicle.Name = dto.Name;
            vehicle.VIN = dto.VIN;
            vehicle.Make = dto.Make;
            vehicle.Model = dto.Model;
            vehicle.Color = dto.Color;
            vehicle.Year = dto.Year;
            vehicle.Plate = dto.Plate;
            vehicle.ExpirationDate = dto.ExpirationDate;
            vehicle.IsInactive = dto.IsInactive;
            vehicle.GroupId = dto.GroupId;
            vehicle.CapacityDetailTypeId = dto.CapacityDetailTypeId;
            vehicle.VehicleTypeId = dto.VehicleTypeId;

            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null) return false;

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}

