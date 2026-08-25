using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Notification.Application.Services;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers.Admin;

/// <summary>
/// The archived notifications, and the trail of who touched them.
/// </summary>
/// <remarks>
/// Archived is the one state the retention policy will not touch, which makes it the only
/// place in the notification tables where rows accumulate without end. Somebody has to be
/// able to look at what is being kept and decide what still deserves to be.
///
/// <para>
/// ⚠️ Administrators only. Everything here either destroys records for good or reveals who
/// destroyed them. The role is compared against <c>ClaimTypes.Role</c>, which the login
/// issues as the numeric <c>RoleId</c>; role 1 is Administrator.
/// </para>
/// </remarks>
[ApiController]
[Route("api/admin/notification")]
[Authorize(Roles = "1")]
public sealed class NotificationAdminController : ControllerBase
{
    private readonly NotificationArchiveService _archiveService;

    private readonly ICurrentUserService _currentUser;

    public NotificationAdminController(
        NotificationArchiveService archiveService,
        ICurrentUserService currentUser)
    {
        _archiveService = archiveService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Everything archived, grouped by the application it was addressed to.
    /// </summary>
    /// <remarks>
    /// ⚠️ The total is not the sum of the groups: a cancellation reaches the patient, the
    /// office and the driver, and shows under each while being one row.
    /// </remarks>
    [HttpGet("archived")]
    public async Task<IActionResult> GetArchived(CancellationToken cancellationToken)
    {
        return Ok(await _archiveService.GetArchivedAsync(cancellationToken));
    }

    /// <summary>
    /// Deletes one archived notification for good, with everything hanging off it.
    /// </summary>
    /// <remarks>
    /// ⚠️ Not reversible, and it only reaches archived rows. A live notification belongs to
    /// the retention policy; deleting one by hand would take a notice off a dispatcher's
    /// screen while they were reading it.
    /// </remarks>
    [HttpDelete("archived/{notificationId:guid}")]
    public async Task<IActionResult> DeleteArchived(
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var deleted = await _archiveService.DeleteArchivedAsync(
            notificationId,
            _currentUser.UserId,
            _currentUser.UserName,
            cancellationToken);

        return deleted ? NoContent() : NotFound();
    }

    /// <summary>Deletes every archived notification. Returns how many went.</summary>
    /// <remarks>⚠️ Not reversible. Recorded against the name of whoever ran it.</remarks>
    [HttpDelete("archived")]
    public async Task<IActionResult> DeleteAllArchived(CancellationToken cancellationToken)
    {
        var deleted = await _archiveService.DeleteAllArchivedAsync(
            _currentUser.UserId,
            _currentUser.UserName,
            cancellationToken);

        return Ok(new { Deleted = deleted });
    }

    /// <summary>
    /// Who archived, purged or deleted, newest first.
    /// </summary>
    /// <remarks>
    /// The trail carries no foreign key to the notifications, on purpose: it has to survive
    /// the rows it describes, or it could not record their deletion.
    /// </remarks>
    [HttpGet("audit")]
    public async Task<IActionResult> GetAudit(
        [FromQuery] int take = 200,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _archiveService.GetAuditAsync(take, cancellationToken));
    }
}
