using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;
using Raphael.Notification.Application.Commands.MarkNotificationViewed;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Queries.GetNotificationById;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;
using Raphael.Notification.Application.Services;
using Raphael.Shared.Definitions.Notifications;
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
    private readonly NotificationArchiveService _archiveService;
    private readonly ICurrentUserService _currentUser;

    public NotificationController(
        GetRecipientNotificationsHandler getRecipientNotificationsHandler,
        GetNotificationByIdHandler getNotificationByIdHandler,
        MarkNotificationViewedHandler markViewedHandler,
        MarkNotificationAcknowledgedHandler markAcknowledgedHandler,
        NotificationArchiveService archiveService,
        ICurrentUserService currentUser)
    {
        _getRecipientNotificationsHandler = getRecipientNotificationsHandler;
        _getNotificationByIdHandler = getNotificationByIdHandler;
        _markViewedHandler = markViewedHandler;
        _markAcknowledgedHandler = markAcknowledgedHandler;
        _archiveService = archiveService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Inbox of a Raphael.Desktop user.
    /// </summary>
    /// <remarks>
    /// Returns what the whole dispatch office shares plus anything addressed to this
    /// user in particular. Office notices are stored once and read by everyone, instead
    /// of one row per dispatcher: the same cancellation seen by twelve people would
    /// otherwise be twelve rows that nobody ever deletes.
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
            return Unauthorized();

        var shared =
            await _getRecipientNotificationsHandler.Handle(
                new GetRecipientNotificationsQuery(
                    UserIdentifierConverter.DesktopAudience,
                    RecipientType.DesktopUser),
                cancellationToken);

        var personal =
            await _getRecipientNotificationsHandler.Handle(
                new GetRecipientNotificationsQuery(
                    UserIdentifierConverter.ToGuid(
                        _currentUser.UserId.Value,
                        RecipientType.DesktopUser),
                    RecipientType.DesktopUser),
                cancellationToken);

        var result = shared
            .Concat(personal)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToList();

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

    /// <summary>
    /// Keeps this notification: the cleanup will never expire or delete it.
    /// </summary>
    /// <remarks>
    /// For a notice somebody will have to answer for later — a trip a patient disputes, a
    /// cancellation under investigation. Any signed-in office user can do it, because
    /// keeping a record is the safe direction: the cost of an unnecessary archive is a row,
    /// and the cost of losing one is a question nobody can answer.
    ///
    /// <para>
    /// It does not put the notice back in anybody's inbox: the reading window does not
    /// move, so it still leaves the office list twelve hours after it was raised.
    /// </para>
    ///
    /// <para>
    /// Note the identifier: this takes the <b>notification</b> id, unlike view and
    /// acknowledge, which take the recipient row. Archiving is a decision about the record
    /// itself, not about one audience's copy of it.
    /// </para>
    /// </remarks>
    [HttpPost("{notificationId:guid}/archive")]
    public async Task<IActionResult> Archive(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var archived = await _archiveService.ArchiveAsync(
            notificationId,
            _currentUser.UserId,
            _currentUser.UserName,
            cancellationToken);

        return archived ? NoContent() : NotFound();
    }

    /// <summary>
    /// Takes the decision back: the notification ages and is deleted like any other.
    /// </summary>
    [HttpPost("{notificationId:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var unarchived = await _archiveService.UnarchiveAsync(
            notificationId,
            _currentUser.UserId,
            _currentUser.UserName,
            cancellationToken);

        return unarchived ? NoContent() : NotFound();
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