using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.UsersService.Services
{
    public class RoleService : IRoleService
    {
        private readonly MediTransContext _context;

        public RoleService(MediTransContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<Role> CreateAsync(Role role)
        {
            _context.Roles.Add(role);
            await _context.SaveChangesAsync();
            return role;
        }

        public async Task<Role?> UpdateAsync(int id, Role role)
        {
            var existing = await _context.Roles.FindAsync(id);
            if (existing == null) return null;

            existing.RoleName = role.RoleName;
            existing.Description = role.Description;

            await _context.SaveChangesAsync();
            return existing;
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
