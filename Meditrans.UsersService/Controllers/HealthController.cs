using Microsoft.AspNetCore.Mvc;

namespace Meditrans.UsersService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Users Service is running");
    }
}
