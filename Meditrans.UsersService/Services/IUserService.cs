using Meditrans.UsersService.Models;

namespace Meditrans.UsersService.Services
{
    public interface IUserService
    {
        IEnumerable<User> GetAll();
        User? GetById(Guid id);
        void Create(User user);
        void Update(User user);
        void Delete(Guid id);
    }
}
