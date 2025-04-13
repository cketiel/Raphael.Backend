using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;
using Meditrans.UsersService.DTOs;

namespace Meditrans.UsersService.Services
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

    /*public interface IUserService
    {
        IEnumerable<User> GetAll();
        User? GetById(Guid id);
        void Create(User user);
        void Update(User user);
        void Delete(Guid id);
    }*/

    /*public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllUsersAsync();
        Task<UserDto?> GetUserByIdAsync(int id);
    }*/
}
