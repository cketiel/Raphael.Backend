using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;
using Raphael.Notification.Application.Commands.MarkNotificationViewed;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Queries.GetNotificationById;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationController : ControllerBase
{
    private readonly GetRecipientNotificationsHandler _getRecipientNotificationsHandler;
    private readonly GetNotificationByIdHandler _getNotificationByIdHandler;
    private readonly MarkNotificationViewedHandler _markViewedHandler;
    private readonly MarkNotificationAcknowledgedHandler _markAcknowledgedHandler;
    private readonly ICurrentUserService _currentUser;

    public NotificationController(
        GetRecipientNotificationsHandler getRecipientNotificationsHandler,
        GetNotificationByIdHandler getNotificationByIdHandler,
        MarkNotificationViewedHandler markViewedHandler,
        MarkNotificationAcknowledgedHandler markAcknowledgedHandler,
        ICurrentUserService currentUser)
    {
        _getRecipientNotificationsHandler = getRecipientNotificationsHandler;
        _getNotificationByIdHandler = getNotificationByIdHandler;
        _markViewedHandler = markViewedHandler;
        _markAcknowledgedHandler = markAcknowledgedHandler;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var command = new GetRecipientNotificationsQuery(
            UserIdentifierConverter.ToGuid(_currentUser.UserId.Value));

        var result =
            await _getRecipientNotificationsHandler.Handle(
                command,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{notificationId:guid}")]
    public async Task<IActionResult> GetNotification(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var query =
            new GetNotificationByIdQuery(notificationId);

        var notification =
            await _getNotificationByIdHandler.Handle(
                query,
                cancellationToken);

        if (notification == null)
            return NotFound();

        return Ok(notification);
    }

    [HttpPost("{recipientId:guid}/view")]
    public async Task<IActionResult> MarkViewed(
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        await _markViewedHandler.Handle(
            new MarkNotificationViewedCommand(recipientId),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{recipientId:guid}/acknowledge")]
    public async Task<IActionResult> MarkAcknowledged(
        Guid recipientId,
        CancellationToken cancellationToken)
    {
        await _markAcknowledgedHandler.Handle(
            new MarkNotificationAcknowledgedCommand(recipientId),
            cancellationToken);

        return NoContent();
    }
}

/*using Microsoft.AspNetCore.Authorization;
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
}*/