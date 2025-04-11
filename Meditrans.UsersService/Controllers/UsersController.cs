using Microsoft.AspNetCore.Mvc;
using Meditrans.UsersService.Services;
using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;


namespace Meditrans.UsersService.Controllers
{
    [ApiController]
    //[Route("api/[controller]")]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService userService)
        {
            _service = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _service.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPost]
        public async Task<IActionResult> Create(User user) => Ok(await _service.CreateAsync(user));

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, User user)
        {
            var updated = await _service.UpdateAsync(id, user);
            return updated == null ? NotFound() : Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok() : NotFound();
        }

        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request)
        {
            if (id != request.UserId)
                return BadRequest("User ID mismatch.");

            var success = await _service.ChangePasswordAsync(request);
            if (!success)
                return BadRequest("Invalid current password or user not found.");

            return Ok("Password changed successfully.");
        }


        // GET: api/users
        /*[HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        // GET: api/users/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user);
        }*/
    }

    /*public class UsersController : ControllerBase
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

    }*/
}
