using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Microsoft.EntityFrameworkCore;
namespace Meditrans.Api.Services
{
    public class VehicleTypeService: IVehicleTypeService
    {
        private readonly RaphaelContext _context;

        public VehicleTypeService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<VehicleType>> GetAllAsync()
        {                  
            return await _context.VehicleTypes.ToListAsync();
        }

        public async Task<VehicleType?> GetByIdAsync(int id)
        {
            return await _context.VehicleTypes.FindAsync(id);
        }

        public async Task<VehicleType> CreateAsync(VehicleType dto)
        {
            var entity = new VehicleType
            {
                Name = dto.Name,
                Description = dto.Description
            };

            _context.VehicleTypes.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<VehicleType?> UpdateAsync(int id, VehicleType dto)
        {
            var entity = await _context.VehicleTypes.FindAsync(id);
            if (entity == null) return null;

            entity.Name = dto.Name;
            entity.Description = dto.Description;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.VehicleTypes.FindAsync(id);
            if (entity == null) return false;

            _context.VehicleTypes.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
