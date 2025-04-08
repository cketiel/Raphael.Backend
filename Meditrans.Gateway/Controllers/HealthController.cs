using Microsoft.AspNetCore.Mvc;

namespace Meditrans.Gateway.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Gateway Service is running");
    }
}
