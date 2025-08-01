using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(int id);
        Task<User> CreateAsync(UserCreateDto user);
        Task<User> UpdateAsync(UserUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task ChangePasswordAsync(ChangePasswordDto dto);

    }
   
}

