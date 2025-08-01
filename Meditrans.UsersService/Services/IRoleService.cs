using Raphael.Shared.Entities;
using Raphael.UsersService.DTOs;

namespace Raphael.UsersService.Services
{
    public interface IRoleService
    {
        Task<IEnumerable<RoleDto>> GetAllAsync();
        Task<RoleDto?> GetByIdAsync(int id);
        Task<RoleDto> CreateAsync(RoleDto roleDto);
        Task<bool> UpdateAsync(int id, RoleDto roleDto);
        Task<bool> DeleteAsync(int id);
    }
}

