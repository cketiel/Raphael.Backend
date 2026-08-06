using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Admin;
using Raphael.Notification.Application.DTOs;
using Raphael.Shared.Services;

namespace Raphael.Api.Controllers.Admin;


[ApiController]
[Route("api/admin/notification/catalog")]
//[Authorize(Roles = "1")]
public sealed class NotificationCatalogController : ControllerBase
{

    private readonly BusinessEventCatalogSeeder _businessEventCatalogSeeder;

    private readonly NotificationRuleCatalogSeeder _notificationRuleCatalogSeeder;

    private readonly NotificationRuleService _notificationRuleService;



    public NotificationCatalogController(
        BusinessEventCatalogSeeder businessEventCatalogSeeder,
        NotificationRuleCatalogSeeder notificationRuleCatalogSeeder,
        NotificationRuleService notificationRuleService)
    {
        _businessEventCatalogSeeder = businessEventCatalogSeeder;

        _notificationRuleCatalogSeeder = notificationRuleCatalogSeeder;

        _notificationRuleService = notificationRuleService;
    }



    /*
    [AllowAnonymous]
    [HttpPost("business-events")]
    public async Task<IActionResult> SeedBusinessEvents(
        CancellationToken cancellationToken)
    {
        try
        {
            await _businessEventCatalogSeeder
                .SeedAsync(cancellationToken);


            return Ok(new
            {
                Message =
                "Business Event Catalog synchronized successfully."
            });
        }
        catch(Exception ex)
        {
            return BadRequest(new
            {
                ex.Message,
                Inner = ex.InnerException?.Message,
                Stack = ex.StackTrace
            });
        }
    }
    */

    [AllowAnonymous]
    [HttpPost("notification-rules")]
    public async Task<IActionResult> SeedNotificationRules(
    [FromQuery] bool updateExisting = false,
    CancellationToken cancellationToken = default)
    {
        try
        {
            await _notificationRuleCatalogSeeder
                .SeedAsync(updateExisting, cancellationToken);

            return Ok(new
            {
                Message = updateExisting
                    ? "Notification Rule Catalog synchronized and updated successfully."
                    : "Notification Rule Catalog inserted successfully."
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
    }


    [AllowAnonymous]
    [HttpGet("rules")]
    public async Task<IActionResult> GetRules()
    {
        var rules =
            await _notificationRuleService.GetAllAsync();


        return Ok(rules);
    }




    [AllowAnonymous]
    [HttpGet("rules/{id:guid}")]
    public async Task<IActionResult> GetRule(Guid id)
    {
        var rule =
            await _notificationRuleService.GetByIdAsync(id);


        if (rule == null)
            return NotFound();


        return Ok(rule);
    }





    [AllowAnonymous]
    [HttpPut("rules")]
    public async Task<IActionResult> UpdateRule(
        UpdateNotificationRuleDto dto)
    {
        try
        {
            await _notificationRuleService.UpdateAsync(dto);

            return NoContent();
        }
        catch (Exception ex)
        {
            return BadRequest(new
            {
                ex.Message,
                Inner = ex.InnerException?.Message
            });
        }
    }





    [AllowAnonymous]
    [HttpPatch("rules/{id:guid}/active")]
    public async Task<IActionResult> SetRuleActive(
        Guid id,
        bool isActive)
    {
        var result =
            await _notificationRuleService
                .SetActiveAsync(id, isActive);



        if (!result)
            return NotFound();


        return NoContent();
    }

}