using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public class UnitService
    {
        private readonly RaphaelContext _context;

        public UnitService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<Unit>> GetAllAsync()
        {
            return await _context.Units.ToListAsync();
        }

        public async Task<Unit?> GetByIdAsync(int id)
        {
            return await _context.Units.FindAsync(id);
        }

        public async Task<Unit> CreateAsync(UnitDto dto)
        {
            var unit = new Unit
            {
                Abbreviation = dto.Abbreviation,
                Description = dto.Description
            };

            _context.Units.Add(unit);
            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<Unit?> UpdateAsync(int id, UnitDto dto)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return null;

            unit.Abbreviation = dto.Abbreviation;
            unit.Description = dto.Description;

            await _context.SaveChangesAsync();
            return unit;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var unit = await _context.Units.FindAsync(id);
            if (unit == null) return false;

            _context.Units.Remove(unit);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}