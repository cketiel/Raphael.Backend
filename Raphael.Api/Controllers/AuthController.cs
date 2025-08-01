using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Raphael.Api.Settings;
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Helpers;
using Microsoft.AspNetCore.Authorization;
using System.Numerics;
using Raphael.Shared.DbContexts;


namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly JwtSettings _jwtSettings;
        private readonly RaphaelContext _context;

        public AuthController(IOptions<JwtSettings> jwtOptions, RaphaelContext context)
        {
            _jwtSettings = jwtOptions.Value;
            _context = context;
        }
        [HttpPost("loginTest")]
        public async Task<IActionResult> LoginTest([FromBody] LoginRequest request)
        {
            return Ok(new { message = "Login successful" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {   
            var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user == null || !PasswordHasher.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized("Invalid username or password.");
            }

            // Here the JWT is generated and returned
            /*var token = _jwtService.GenerateToken(user); 
            return Ok(new { token });*/

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.RoleId.ToString()),

                 new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                 new Claim(JwtRegisteredClaimNames.UniqueName, user.Username),
            

                 new Claim("UserId", user.Id.ToString()),
                 new Claim("Username", user.Username),
                 new Claim("Role", user.RoleId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresInMinutes),
                signingCredentials: creds
            );

            //var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token),
                userId = user.Id.ToString(),
                username = user.Username,
                //userfullname = user.FullName,
                role = user.RoleId.ToString(),
                isSuccess = true
            });
        }

    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}

