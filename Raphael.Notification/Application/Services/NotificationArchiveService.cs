using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raphael.Notification.Application.DTOs;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Services;

/// <summary>
/// Keeping notifications, letting them go again, and recording who decided which.
/// </summary>
/// <remarks>
/// Archiving is the one decision that overrides the retention policy: an archived
/// notification is never expired and never deleted. That makes it the only place in the
/// notification tables where rows accumulate forever, so the same service that creates
/// them also has to be able to list and remove them — and every one of those actions is
/// written down against a name.
/// </remarks>
public sealed class NotificationArchiveService
{
    private readonly RaphaelContext _context;

    private readonly ILogger<NotificationArchiveService> _logger;

    public NotificationArchiveService(
        RaphaelContext context,
        ILogger<NotificationArchiveService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Marks a notification to be kept. Returns false when there is no such notification.
    /// </summary>
    public async Task<bool> ArchiveAsync(
        Guid notificationId,
        int? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
            return false;

        if (notification.Status.Id == NotificationStatus.Archived.Id)
            return true;

        notification.Archive();

        await RecordAsync(
            NotificationAdminActions.Archive,
            userId,
            username,
            notificationId,
            details: notification.BusinessEventCode,
            cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>Takes the decision back, so the notification ages like any other.</summary>
    public async Task<bool> UnarchiveAsync(
        Guid notificationId,
        int? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId, cancellationToken);

        if (notification is null)
            return false;

        notification.Unarchive();

        await RecordAsync(
            NotificationAdminActions.Unarchive,
            userId,
            username,
            notificationId,
            details: notification.BusinessEventCode,
            cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }

    /// <summary>
    /// Everything archived, grouped by the application it was addressed to.
    /// </summary>
    /// <remarks>
    /// A notification with several audiences appears under each of them, which is why the
    /// total is counted separately rather than summed from the groups.
    /// </remarks>
    public async Task<ArchivedNotificationsDto> GetArchivedAsync(
        CancellationToken cancellationToken = default)
    {
        var archived = await _context.Notifications
            .AsNoTracking()
            .Include(n => n.Recipients)
            .Where(n => n.StatusId == NotificationStatus.Archived.Id)
            .OrderByDescending(n => n.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        if (archived.Count == 0)
            return new ArchivedNotificationsDto();

        var ids = archived.Select(n => n.Id).ToList();

        // Who kept each one, from the trail. Older rows archived before this existed simply
        // have no entry, and say so by leaving the name empty.
        var archivedBy = await _context.NotificationAdminAudits
            .AsNoTracking()
            .Where(a => a.Action == NotificationAdminActions.Archive
                        && a.NotificationId != null
                        && ids.Contains(a.NotificationId.Value))
            .GroupBy(a => a.NotificationId!.Value)
            .Select(g => g
                .OrderByDescending(a => a.PerformedAtUtc)
                .First())
            .ToListAsync(cancellationToken);

        var byNotification = archivedBy.ToDictionary(a => a.NotificationId!.Value);

        var items = archived
            .Select(n =>
            {
                byNotification.TryGetValue(n.Id, out var audit);

                return new ArchivedNotificationDto
                {
                    Id = n.Id,
                    BusinessEventCode = n.BusinessEventCode,
                    Title = n.Title,
                    Message = n.Message,
                    CreatedAtUtc = n.CreatedAtUtc,
                    ExpiresAtUtc = n.ExpiresAtUtc,
                    Audiences = n.Recipients
                        .Select(r => r.RecipientType.Name)
                        .Distinct()
                        .OrderBy(name => name)
                        .ToList(),
                    ArchivedByUsername = audit?.PerformedByUsername,
                    ArchivedAtUtc = audit?.PerformedAtUtc
                };
            })
            .ToList();

        var groups = items
            .SelectMany(item => item.Audiences.Select(audience => (audience, item)))
            .GroupBy(pair => pair.audience)
            .Select(group => new ArchivedNotificationGroupDto
            {
                Audience = group.Key,
                Count = group.Count(),
                Items = group.Select(pair => pair.item).ToList()
            })
            .OrderBy(group => group.Audience)
            .ToList();

        // Rows with no audience at all would vanish from every group. They are older data,
        // but a list that silently omits rows is not a list anybody can act on.
        var orphans = items.Where(item => item.Audiences.Count == 0).ToList();

        if (orphans.Count > 0)
        {
            groups.Add(new ArchivedNotificationGroupDto
            {
                Audience = "Unassigned",
                Count = orphans.Count,
                Items = orphans
            });
        }

        return new ArchivedNotificationsDto
        {
            Total = items.Count,
            Groups = groups
        };
    }

    /// <summary>
    /// Deletes one archived notification for good, with its recipients and metadata.
    /// </summary>
    /// <remarks>
    /// ⚠️ Only reaches archived rows. Everything else belongs to the retention policy, and
    /// letting an administrator delete a live notification by hand would take a notice off
    /// a dispatcher's screen while they were reading it.
    /// </remarks>
    public async Task<bool> DeleteArchivedAsync(
        Guid notificationId,
        int? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == notificationId
                     && n.StatusId == NotificationStatus.Archived.Id,
                cancellationToken);

        if (notification is null)
            return false;

        var eventCode = notification.BusinessEventCode;

        // Recorded before the delete: afterwards there is nothing left to describe.
        await RecordAsync(
            NotificationAdminActions.DeleteArchived,
            userId,
            username,
            notificationId,
            details: eventCode,
            cancellationToken: cancellationToken);

        _context.Notifications.Remove(notification);

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogWarning(
            "Archived notification {NotificationId} ({EventCode}) deleted by {User}.",
            notificationId,
            eventCode,
            username ?? "unknown");

        return true;
    }

    /// <summary>Deletes every archived notification. Returns how many went.</summary>
    public async Task<int> DeleteAllArchivedAsync(
        int? userId,
        string? username,
        CancellationToken cancellationToken = default)
    {
        var count = await _context.Notifications
            .CountAsync(
                n => n.StatusId == NotificationStatus.Archived.Id,
                cancellationToken);

        if (count == 0)
            return 0;

        await RecordAsync(
            NotificationAdminActions.DeleteArchivedAll,
            userId,
            username,
            notificationId: null,
            affectedCount: count,
            cancellationToken: cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _context.Notifications
            .Where(n => n.StatusId == NotificationStatus.Archived.Id)
            .ExecuteDeleteAsync(cancellationToken);

        _logger.LogWarning(
            "All {Count} archived notifications deleted by {User}.",
            count,
            username ?? "unknown");

        return count;
    }

    /// <summary>The trail, newest first.</summary>
    public async Task<IReadOnlyList<NotificationAdminAuditDto>> GetAuditAsync(
        int take = 200,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationAdminAudits
            .AsNoTracking()
            .OrderByDescending(a => a.PerformedAtUtc)
            .Take(Math.Clamp(take, 1, 1000))
            .Select(a => new NotificationAdminAuditDto
            {
                Id = a.Id,
                Action = a.Action,
                PerformedByUserId = a.PerformedByUserId,
                PerformedByUsername = a.PerformedByUsername,
                PerformedAtUtc = a.PerformedAtUtc,
                NotificationId = a.NotificationId,
                AffectedCount = a.AffectedCount,
                Details = a.Details
            })
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Adds a trail entry. Does not save: the caller decides the transaction boundary, so
    /// the record and what it records land together or not at all.
    /// </summary>
    public async Task RecordAsync(
        string action,
        int? userId,
        string? username,
        Guid? notificationId = null,
        int affectedCount = 1,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        await _context.NotificationAdminAudits.AddAsync(
            new NotificationAdminAudit(
                action,
                userId,
                username,
                notificationId,
                affectedCount,
                details),
            cancellationToken);
    }

    /// <summary>Adds a trail entry and saves it on its own.</summary>
    public async Task RecordAndSaveAsync(
        string action,
        int? userId,
        string? username,
        Guid? notificationId = null,
        int affectedCount = 1,
        string? details = null,
        CancellationToken cancellationToken = default)
    {
        await RecordAsync(
            action,
            userId,
            username,
            notificationId,
            affectedCount,
            details,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
