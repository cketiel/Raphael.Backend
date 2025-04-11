using Meditrans.Shared.Entities;

namespace Meditrans.UsersService.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<Role>> GetAllAsync();
        Task<Role?> GetByIdAsync(int id);
        Task<Role> CreateAsync(Role role);
        Task<Role?> UpdateAsync(int id, Role role);
        Task<bool> DeleteAsync(int id);
    }
}
