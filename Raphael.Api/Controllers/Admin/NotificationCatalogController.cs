using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Admin;
using Raphael.Notification.Application.DTOs;
using Raphael.Shared.Services;
using NotificationRetentionService = Raphael.Notification.Application.Services.NotificationRetentionService;

namespace Raphael.Api.Controllers.Admin;


[ApiController]
[Route("api/admin/notification/catalog")]
//[Authorize(Roles = "1")]
public sealed class NotificationCatalogController : ControllerBase
{

    private readonly BusinessEventCatalogSeeder _businessEventCatalogSeeder;

    private readonly NotificationRuleCatalogSeeder _notificationRuleCatalogSeeder;

    private readonly NotificationRuleService _notificationRuleService;

    private readonly NotificationRetentionService _retentionService;



    public NotificationCatalogController(
        BusinessEventCatalogSeeder businessEventCatalogSeeder,
        NotificationRuleCatalogSeeder notificationRuleCatalogSeeder,
        NotificationRuleService notificationRuleService,
        NotificationRetentionService retentionService)
    {
        _businessEventCatalogSeeder = businessEventCatalogSeeder;

        _notificationRuleCatalogSeeder = notificationRuleCatalogSeeder;

        _notificationRuleService = notificationRuleService;

        _retentionService = retentionService;
    }



    /// <summary>
    /// Brings the business event catalog in the database in line with the one in code.
    /// </summary>
    /// <remarks>
    /// Safe to re-run: it inserts what is missing instead of duplicating what is there.
    /// It used to insert blindly, which is why it had been commented out.
    /// </remarks>
    /// <param name="updateExisting">
    /// Also refresh the names and descriptions of the events already stored.
    /// </param>
    [AllowAnonymous]
    [HttpPost("business-events")]
    public async Task<IActionResult> SeedBusinessEvents(
        [FromQuery] bool updateExisting = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _businessEventCatalogSeeder
                .SeedAsync(updateExisting, cancellationToken);


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


    /// <summary>
    /// Switches every rule of one business event on or off, whatever its audience.
    /// </summary>
    /// <remarks>
    /// Silences one notice across all the applications it reaches, without hunting down
    /// the rule of each audience.
    /// </remarks>
    [AllowAnonymous]
    [HttpPatch("events/{businessEventCode}/active")]
    public async Task<IActionResult> SetEventActive(
        string businessEventCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var affected = await _notificationRuleService
            .SetEventActiveAsync(businessEventCode, isActive, cancellationToken);


        if (affected == 0)
            return NotFound();


        return Ok(new { BusinessEventCode = businessEventCode, IsActive = isActive, Rules = affected });
    }


    /// <summary>
    /// Switches every rule of a whole family of events on or off.
    /// </summary>
    /// <remarks>
    /// The emergency stop. When a family of notices turns out to be wrong or too noisy,
    /// this silences all of it in one action instead of picking rules off a list.
    /// </remarks>
    [AllowAnonymous]
    [HttpPatch("groups/{groupCode}/active")]
    public async Task<IActionResult> SetGroupActive(
        string groupCode,
        bool isActive,
        CancellationToken cancellationToken)
    {
        var affected = await _notificationRuleService
            .SetGroupActiveAsync(groupCode, isActive, cancellationToken);


        if (affected == 0)
            return NotFound();


        return Ok(new { GroupCode = groupCode, IsActive = isActive, Rules = affected });
    }


    /// <summary>
    /// Runs the notification cleanup out of turn.
    /// </summary>
    /// <remarks>
    /// The same pass a background worker runs every night: it expires what is past due
    /// and deletes what expired long enough ago. Exposed for when it has to happen now.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("retention/run")]
    public async Task<IActionResult> RunRetention(
        CancellationToken cancellationToken)
    {
        var result = await _retentionService.RunAsync(cancellationToken);

        return Ok(new
        {
            result.Expired,
            result.Deleted
        });
    }

}