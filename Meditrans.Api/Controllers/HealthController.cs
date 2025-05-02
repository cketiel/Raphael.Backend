using Microsoft.AspNetCore.Mvc;

namespace Meditrans.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok("Api Service is running");
    }
}
