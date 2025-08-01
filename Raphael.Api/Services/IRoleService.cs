using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;


namespace Raphael.Api.Services
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

