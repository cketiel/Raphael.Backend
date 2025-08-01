using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Api.Services
{
    public class CapacityDetailTypeService : ICapacityDetailTypeService
    {
        private readonly RaphaelContext _context;

        public CapacityDetailTypeService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<CapacityDetailType>> GetAllAsync()
        {
            return await _context.CapacityDetailTypes.ToListAsync();
        }

        public async Task<CapacityDetailType?> GetByIdAsync(int id)
        {
            return await _context.CapacityDetailTypes.FindAsync(id);
        }

        public async Task<CapacityDetailType> CreateAsync(CapacityDetailTypeDto dto)
        {
            var entity = new CapacityDetailType
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.CapacityDetailTypes.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<CapacityDetailType?> UpdateAsync(int id, CapacityDetailTypeDto dto)
        {
            var entity = await _context.CapacityDetailTypes.FindAsync(id);
            if (entity == null) return null;

            entity.Name = dto.Name;
            entity.Description = dto.Description;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.CapacityDetailTypes.FindAsync(id);
            if (entity == null) return false;

            _context.CapacityDetailTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
