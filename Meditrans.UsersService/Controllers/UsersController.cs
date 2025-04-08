using Microsoft.AspNetCore.Mvc;
using Meditrans.UsersService.Models;

namespace Meditrans.UsersService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private static readonly List<User> _users = new();

    [HttpGet]
    public ActionResult<IEnumerable<User>> GetAllUsers() => Ok(_users);

    [HttpPost]
    public ActionResult CreateUser(User user)
    {
        user.Id = Guid.NewGuid();
        _users.Add(user);
        return CreatedAtAction(nameof(GetAllUsers), user);
    }
}
