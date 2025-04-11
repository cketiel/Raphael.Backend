using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;
using Meditrans.UsersService.DTOs;

namespace Meditrans.UsersService.Services
{
    public interface IUserService
    {
        Task<IEnumerable<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(int id);
        Task<User> CreateAsync(User user);
        Task<User?> UpdateAsync(int id, User user);
        Task<bool> DeleteAsync(int id);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
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
