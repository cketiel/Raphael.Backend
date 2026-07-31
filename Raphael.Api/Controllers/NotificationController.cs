using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Models.Notifications;
using Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;
using Raphael.Notification.Application.Commands.MarkNotificationViewed;
using Raphael.Notification.Application.Commands.ProcessBusinessEvent;
using Raphael.Notification.Application.Queries.GetNotificationById;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;

namespace Raphael.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly ProcessBusinessEventHandler _handler;
    private readonly GetNotificationByIdHandler _getNotificationByIdHandler;
    private readonly GetRecipientNotificationsHandler _getRecipientNotificationsHandler;
    private readonly MarkNotificationViewedHandler _markViewedHandler;
    private readonly MarkNotificationAcknowledgedHandler _acknowledgeHandler;

    public NotificationController(
    ProcessBusinessEventHandler handler,
    GetNotificationByIdHandler getNotificationByIdHandler,
    GetRecipientNotificationsHandler getRecipientNotificationsHandler,
    MarkNotificationViewedHandler markViewedHandler,
    MarkNotificationAcknowledgedHandler acknowledgeHandler)
    {
        _handler = handler;

        _getNotificationByIdHandler = getNotificationByIdHandler;

        _getRecipientNotificationsHandler = getRecipientNotificationsHandler;
        _markViewedHandler = markViewedHandler;
        _acknowledgeHandler = acknowledgeHandler;
    }


    [HttpPost("events")]
    public async Task<IActionResult> ProcessEvent(
        [FromBody] ProcessBusinessEventRequest request,
        CancellationToken cancellationToken)
    {

        var command = new ProcessBusinessEventCommand(
            request.BusinessEventCode,
            request.EntityId,
            request.EntityType,
            request.Data);


        await _handler.Handle(
            command,
            cancellationToken);


        return Ok();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(
    Guid id,
    CancellationToken cancellationToken)
    {
        var query = new GetNotificationByIdQuery(id);


        var result =
            await _getNotificationByIdHandler.Handle(
                query,
                cancellationToken);


        if (result == null)
        {
            return NotFound();
        }


        return Ok(result);
    }

    [HttpGet("recipient/{recipientId:guid}")]
    public async Task<IActionResult> GetByRecipient(
    Guid recipientId,
    CancellationToken cancellationToken)
    {
        var query =
            new GetRecipientNotificationsQuery(
                recipientId);


        var result =
            await _getRecipientNotificationsHandler.Handle(
                query,
                cancellationToken);


        return Ok(result);
    }

    [HttpPut("{recipientId:guid}/viewed")]
    public async Task<IActionResult> MarkViewed(
    Guid recipientId,
    CancellationToken cancellationToken)
    {
        var command =
            new MarkNotificationViewedCommand(
                recipientId);


        await _markViewedHandler.Handle(
            command,
            cancellationToken);


        return NoContent();
    }

    [HttpPut("{recipientId:guid}/acknowledge")]
    public async Task<IActionResult> Acknowledge(
    Guid recipientId,
    CancellationToken cancellationToken)
    {
        var command =
            new MarkNotificationAcknowledgedCommand(
                recipientId);


        await _acknowledgeHandler.Handle(
            command,
            cancellationToken);


        return NoContent();
    }
}