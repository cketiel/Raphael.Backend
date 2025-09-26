using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using System.Threading.Tasks;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProvidersController : ControllerBase
    {
        private readonly IProviderService _providerService;

        public ProvidersController(IProviderService providerService)
        {
            _providerService = providerService;
        }

        // GET: api/providers/contact
        [HttpGet("contact")]
        public async Task<ActionResult<ProviderDto>> GetContactProvider()
        {
            var provider = await _providerService.GetContactProviderAsync();
            if (provider == null)
            {
                
                return NotFound();
            }
            return Ok(provider);
        }

        // PUT: api/providers/contact
        [HttpPut("contact")]
        public async Task<IActionResult> UpdateContactProvider([FromBody] ProviderDto providerDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _providerService.UpdateContactProviderAsync(providerDto);

            if (!success)
            {
                return NotFound("Contact provider not found and could not be updated.");
            }

            return NoContent(); // Hide the Text Customer and Send Dispatch Message actions
        }
    }
}