using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;

namespace Meditrans.Api.Services
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
