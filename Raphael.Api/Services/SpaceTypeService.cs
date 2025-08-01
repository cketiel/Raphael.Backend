using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities;
using Raphael.Shared.Dtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;

namespace Raphael.Api.Services
{
    public class SpaceTypeService
    {
        private readonly RaphaelContext _context;

        public SpaceTypeService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<SpaceType>> GetAllAsync()
        {
            return await _context.SpaceTypes
                .Include(st => st.CapacityType)
                .ToListAsync();
        }

        public async Task<SpaceType?> GetByIdAsync(int id)
        {
            return await _context.SpaceTypes
                .Include(st => st.CapacityType)
                .FirstOrDefaultAsync(st => st.Id == id);
        }

        public async Task<SpaceType> CreateAsync(SpaceTypeDto dto)
        {       
            var spaceType = new SpaceType
            {
                Name = dto.Name,
                Description = dto.Description,
                LoadTime = dto.LoadTime,
                UnloadTime = dto.UnloadTime,
                CapacityTypeId = dto.CapacityTypeId,
                IsActive = dto.IsActive
            };

            _context.SpaceTypes.Add(spaceType);
            await _context.SaveChangesAsync();
            return spaceType;          
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var spaceType = await _context.SpaceTypes.FindAsync(id);
            if (spaceType == null) return false;

            _context.SpaceTypes.Remove(spaceType);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<SpaceType?> GetByNameAsync(string name)
        {
            return await _context.SpaceTypes
                 .Include(st => st.CapacityType)
                 .FirstOrDefaultAsync(st => st.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> NameExists(string name)
        {
            return await _context.SpaceTypes.AnyAsync(s => s.Name == name);
        }
    }
}

