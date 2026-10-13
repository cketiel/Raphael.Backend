using Raphael.Api.Services.Auth;
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
        /// <summary>
        /// RoleId of an administrator. The token carries role identifiers, not names.
        /// Writes are admin-only (B20): without it any office token, a driver's included,
        /// could create an admin or take over one by changing its password. Reads stay open
        /// to the office, because Desktop loads users for every role (startup, Reports, the
        /// route editor).
        /// </summary>
        private const string AdminRoleId = "1";

        private readonly IUserService _service;
        private readonly ICurrentUserService _currentUser;

        /// <summary>The caller's tenant when isolation is on; null for Raphael's office and everybody else.</summary>
        private int? TenantOfCaller => _currentUser.TenancyEnforced ? _currentUser.TenantId : null;

        /// <summary>
        /// True when the caller may reach this user: always for Raphael's office, and for a tenant's
        /// admin only inside its own company. An unknown id answers false, so it reads as 404.
        /// </summary>
        private async Task<bool> InCallersTenantAsync(int userId)
        {
            if (TenantOfCaller is not int tenant) return true;
            var (exists, providerId) = await _service.ProviderOfAsync(userId);
            return exists && providerId == tenant;
        }

        public UsersController(IUserService userService, ICurrentUserService currentUser)
        {
            _service = userService;
            _currentUser = currentUser;
        }

        // Every user (UserDto, no password hash): never to a clinic, which holds its token in the browser.
        [NotForClinicUsers]
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            var users = await _service.GetAllAsync();
            // A tenant's admin sees its own company's people and nobody else (RE-049).
            if (TenantOfCaller is int tenant)
                users = users.Where(u => u.ProviderId == tenant).ToList();
            return Ok(users);
        }
        //public async Task<IActionResult> GetAll() => Ok(await _service.GetAllAsync());

        [NotForClinicUsers]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(int id)
        {
            if (!await InCallersTenantAsync(id)) return NotFound();
            var user = await _service.GetByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        [NotForClinicUsers]
        [Authorize(Roles = AdminRoleId)]
        [HttpPost("create")]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            try
            {
                // RE-049, no resale: a tenant's admin only creates people of its own company. The
                // Provider and the clinic come from the token, never from the request.
                if (TenantOfCaller is int tenant)
                {
                    dto.ProviderId = tenant;
                    dto.IntegratorId = null;
                }

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
        [Authorize(Roles = AdminRoleId)]
        [HttpPut("update")]
        public async Task<IActionResult> UpdateUser([FromBody] UserUpdateDto dto)
        {
            try
            {
                if (TenantOfCaller is int tenant)
                {
                    if (!await InCallersTenantAsync(dto.Id)) return NotFound();
                    dto.ProviderId = tenant;
                    dto.IntegratorId = null;
                }

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
        [Authorize(Roles = AdminRoleId)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!await InCallersTenantAsync(id)) return NotFound();
            var deleted = await _service.DeleteAsync(id);
            return deleted ? Ok() : NotFound();
        }

        [AllowApps(ClientApps.Desktop, ClientApps.Booking)]
        [HttpPut("change-password")]
        [AllowWhilePasswordChangeRequired]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            // The user id travels in the body, and a clinic holds its token in the browser: without
            // this a clinic could change, or keep guessing, another user's password. A clinic user
            // changes only its own, and so does an external Provider's.
            if ((_currentUser.IntegratorId != null || _currentUser.IsExternalProvider || _currentUser.MustChangePassword)
                && dto.UserId != _currentUser.UserId)
            {
                return Forbid();
            }

            // B20: someone else's password only by an office admin (Desktop, Admin → Users).
            // Before, any office token, a driver's included, could reset the admin's and sign in as it.
            if (dto.UserId != _currentUser.UserId && _currentUser.RoleId?.ToString() != AdminRoleId)
            {
                return Forbid();
            }

            // ...and a tenant's admin only for the people of its own company (RE-049).
            if (dto.UserId != _currentUser.UserId && !await InCallersTenantAsync(dto.UserId))
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

