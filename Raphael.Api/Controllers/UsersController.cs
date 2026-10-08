using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Attributes;
using Raphael.Api.Services;
using Raphael.Shared.Entities;
using Raphael.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Raphael.Shared.Interfaces;


namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _service;
        private readonly ICurrentUserService _currentUser;

        public UsersController(IUserService userService, ICurrentUserService currentUser)
        {
            _service = userService;
            _currentUser = currentUser;
        }

        // Every user, with its password hash: never to a clinic, which holds its token in the browser.
        [NotForClinicUsers]
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }
        //public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [NotForClinicUsers]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            var user = await _service.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [NotForClinicUsers]
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

        [NotForClinicUsers]
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


        [NotForClinicUsers]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok() : NotFound();
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            // The user id travels in the body, and a clinic holds its token in the browser: without
            // this a clinic could change, or keep guessing, another user's password. A clinic user
            // changes only its own. The office is left as it was (decided 2026-10-07).
            if (_currentUser.IntegratorId != null && dto.UserId != _currentUser.UserId)
            {
                return Forbid();
            }

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

