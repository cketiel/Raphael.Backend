using Microsoft.AspNetCore.Mvc;

namespace Raphael.Notifications.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Notifications Service is running");
    }
}

