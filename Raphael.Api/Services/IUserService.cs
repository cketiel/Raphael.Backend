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
        /// <summary>An administrator sets someone else's password and ends that user's sessions.</summary>
        Task<bool> ResetPasswordAsync(int userId, string newPassword);
        /// <summary>The Provider a user belongs to, and whether the user exists at all (RE-049).</summary>
        Task<(bool Exists, int? ProviderId)> ProviderOfAsync(int userId);

    }
   
}

