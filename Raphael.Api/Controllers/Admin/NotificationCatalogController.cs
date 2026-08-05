using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Shared.Services;

namespace Raphael.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/notification/catalog")]
//[Authorize(Roles = "1")]
public sealed class NotificationCatalogController : ControllerBase
{
    private readonly BusinessEventCatalogSeeder _businessEventCatalogSeeder;

    public NotificationCatalogController(
        BusinessEventCatalogSeeder businessEventCatalogSeeder)
    {
        _businessEventCatalogSeeder = businessEventCatalogSeeder;
    }

    // This endpoint is commented out to prevent accidental execution. Uncomment it if you want to enable the seeding of business events.
    /*
    [AllowAnonymous]
    [HttpPost("business-events")]
    public async Task<IActionResult> SeedBusinessEvents(
        CancellationToken cancellationToken)
    {
        
        try
        {
            await _businessEventCatalogSeeder.SeedAsync(cancellationToken);

            return Ok(new
            {
                Message = "Business Event Catalog synchronized successfully."
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                ex.Message,
                Inner = ex.InnerException?.Message,
                Stack = ex.StackTrace
            });
        }
    }*/
}