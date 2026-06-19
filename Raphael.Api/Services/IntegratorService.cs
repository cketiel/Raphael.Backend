using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Services
{
    public class IntegratorService : IIntegratorService
    {
        private readonly RaphaelContext _context;

        public IntegratorService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<IntegratorDto>> GetAllAsync()
        {
            return await _context.Integrators
                .Select(i => new IntegratorDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    ApiKey = i.ApiKey,
                    IsActive = i.IsActive,
                    Created = i.Created
                }).ToListAsync();
        }

        public async Task<IntegratorDto?> GetByIdAsync(int id)
        {
            var i = await _context.Integrators.FindAsync(id);
            if (i == null) return null;
            return new IntegratorDto
            {
                Id = i.Id,
                Name = i.Name,
                ApiKey = i.ApiKey,
                IsActive = i.IsActive,
                Created = i.Created
            };
        }

        public async Task<IntegratorDto> CreateAsync(IntegratorDto dto)
        {
            var integrator = new Integrator
            {
                Name = dto.Name,
                IsActive = true,
                Created = DateTime.UtcNow,
                ApiKey = GenerateKey() // Automatic generation
            };

            _context.Integrators.Add(integrator);
            await _context.SaveChangesAsync();
            dto.Id = integrator.Id;
            dto.ApiKey = integrator.ApiKey;
            return dto;
        }

        public async Task<bool> UpdateAsync(int id, IntegratorDto dto)
        {
            var existing = await _context.Integrators.FindAsync(id);
            if (existing == null) return false;

            existing.Name = dto.Name;
            existing.IsActive = dto.IsActive;

            if (dto.RegenerateApiKey)
            {
                existing.ApiKey = GenerateKey();
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var i = await _context.Integrators.FindAsync(id);
            if (i == null) return false;
            _context.Integrators.Remove(i);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateKey()
        {
            // Format: itg_ + long random string
            return "itg_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }
    }
}