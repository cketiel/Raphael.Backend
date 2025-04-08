using Microsoft.AspNetCore.Mvc;

namespace Meditrans.GpsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("GPS Service is running");
    }
}
