using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Services
{
    public class RoleService : IRoleService
    {
        private readonly RaphaelContext _context;

        public RoleService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<RoleDto>> GetAllAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Description = r.Description
                })
                .ToListAsync();
        }

        public async Task<RoleDto?> GetByIdAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            return role == null ? null : new RoleDto
            {
                Id = role.Id,
                RoleName = role.RoleName,
                Description = role.Description
            };
        }

        public async Task<RoleDto> CreateAsync(RoleDto roleDto)
        {
            var role = new Role
            {
                RoleName = roleDto.RoleName,
                Description = roleDto.Description
            };

            _context.Roles.Add(role);
            await _context.SaveChangesAsync();

            roleDto.Id = role.Id;
            return roleDto;
        }

        public async Task<bool> UpdateAsync(int id, RoleDto roleDto)
        {
            var existing = await _context.Roles.FindAsync(id);
            if (existing == null) return false;

            existing.RoleName = roleDto.RoleName;
            existing.Description = roleDto.Description;

            _context.Roles.Update(existing);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null) return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}

