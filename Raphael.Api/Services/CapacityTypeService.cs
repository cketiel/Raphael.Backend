using Raphael.Shared.DbContexts;
using Raphael.Shared.Dtos;
using Raphael.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Services
{
    public class CapacityTypeService
    {
        private readonly RaphaelContext _context;

        public CapacityTypeService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<CapacityType>> GetAllAsync()
        {
            return await _context.Capacities.ToListAsync();
        }

        public async Task<CapacityType?> GetByIdAsync(int id)
        {
            return await _context.Capacities.FindAsync(id);
        }

        public async Task<CapacityType> CreateAsync(CapacityTypeDto dto)
        {
            var entity = new CapacityType
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.Capacities.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<CapacityType?> UpdateAsync(int id, CapacityTypeDto dto)
        {
            var entity = await _context.Capacities.FindAsync(id);
            if (entity == null) return null;

            entity.Name = dto.Name;
            entity.Description = dto.Description;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Capacities.FindAsync(id);
            if (entity == null) return false;

            _context.Capacities.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

