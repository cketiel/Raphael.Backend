using Microsoft.AspNetCore.Mvc;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Api Service is running");
    }
}

