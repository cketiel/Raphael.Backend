using Microsoft.AspNetCore.Mvc;
using Meditrans.Api.Services;
using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;


namespace Meditrans.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;

        public UsersController(IUserService userService)
        {
            _service = userService;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }
        //public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _service.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            try
            {
                var createdUser = await _service.CreateAsync(dto);

                return Ok(new
                {
                    createdUser.Id,
                    createdUser.FullName,
                    createdUser.Username,
                    createdUser.RoleId,
                    createdUser.IsActive
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateDto dto)
        {
            try
            {
                var updatedUser = await _service.UpdateAsync(dto);

                return Ok(new
                {
                    updatedUser.Id,
                    updatedUser.FullName,
                    updatedUser.Username,
                    updatedUser.RoleId,
                    updatedUser.IsActive
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok() : NotFound();
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                await _service.ChangePasswordAsync(dto);
                return Ok(new { message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // This method reads the claims directly from the HttpContext.User (which is automatically completed if the JWT is valid).
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
