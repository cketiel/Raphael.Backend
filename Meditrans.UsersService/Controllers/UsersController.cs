using Microsoft.AspNetCore.Mvc;
using Meditrans.UsersService.Models;
using Meditrans.UsersService.Services;
using Microsoft.AspNetCore.Authorization;
using Meditrans.UsersService.Data;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.UsersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UsersDbContext _context;

        public UsersController(IUserService userService, UsersDbContext context)
        {
            _userService = userService;
            _context = context;
        }
        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            return Ok(users);
        }

        /*[HttpGet]
        public IActionResult GetAll() => Ok(_userService.GetAll());

        [HttpGet("test")]
        public IActionResult Test() => Ok(_context.Users.ToList());*/

        [HttpGet("{id}")]
        public IActionResult GetById(Guid id)
        {
            var user = _userService.GetById(id);
            if (user == null) return NotFound();
            return Ok(user);
        }

        [HttpPost]
        public IActionResult Create(User user)
        {
            _userService.Create(user);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        [HttpPut("{id}")]
        public IActionResult Update(Guid id, User user)
        {
            if (id != user.Id) return BadRequest();
            _userService.Update(user);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(Guid id)
        {
            _userService.Delete(id);
            return NoContent();
        }

        // Este método lee los claims directamente del HttpContext.User(que se completa automáticamente si el JWT es válido).
        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            var userId = User.FindFirst("UserId")?.Value;
            var username = User.FindFirst("Username")?.Value;
            var role = User.FindFirst("Role")?.Value;

            if (userId == null || username == null || role == null)
                return Unauthorized();

            return Ok(new
            {
                UserId = userId,
                Username = username,
                Role = role
            });
        }

        [Authorize]
        [HttpGet("secure-data")]
        public IActionResult GetSecureData()
        {
            return Ok("You have access to this secured endpoint.");
        }

    }
}
