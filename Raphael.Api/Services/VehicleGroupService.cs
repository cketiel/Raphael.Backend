using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Api.Services
{
    public class VehicleGroupService
    {
        private readonly RaphaelContext _context;

        public VehicleGroupService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleGroup>> GetAllAsync()
        {
            return await _context.VehicleGroups.ToListAsync();
        }

        public async Task<VehicleGroup?> GetByIdAsync(int id)
        {
            return await _context.VehicleGroups.FindAsync(id);
        }

        public async Task<VehicleGroup> CreateAsync(VehicleGroupDto dto)
        {
            var group = new VehicleGroup
            {
                Name = dto.Name,
                Description = dto.Description,
                Color = dto.Color
            };

            _context.VehicleGroups.Add(group);
            await _context.SaveChangesAsync();

            return group;
        }

        public async Task<VehicleGroup?> UpdateAsync(int id, VehicleGroupDto dto)
        {
            var group = await _context.VehicleGroups.FindAsync(id);
            if (group == null) return null;

            group.Name = dto.Name;
            group.Description = dto.Description;
            group.Color = dto.Color;

            await _context.SaveChangesAsync();
            return group;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var group = await _context.VehicleGroups.FindAsync(id);
            if (group == null) return false;

            _context.VehicleGroups.Remove(group);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
