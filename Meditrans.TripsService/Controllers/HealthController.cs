using Microsoft.AspNetCore.Mvc;

namespace Meditrans.TripsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Trips Service is running");
    }
}
