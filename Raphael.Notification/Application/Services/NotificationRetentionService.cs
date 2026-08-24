using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;

namespace Raphael.Notification.Application.Services;

/// <summary>
/// What a cleanup run did.
/// </summary>
/// <param name="Expired">Notifications whose reading window closed.</param>
/// <param name="Deleted">Notifications removed for good, with their child rows.</param>
public sealed record NotificationRetentionResult(
    int Expired,
    int Deleted);

/// <summary>
/// Keeps the notification tables from growing without end.
/// </summary>
/// <remarks>
/// Every scheduled trip, every arrival and every cancellation writes a row. Without a
/// cleanup, a system carrying a few thousand trips a month accumulates them forever, and
/// the inbox of a patient who has been travelling for two years becomes a table scan.
///
/// <para>
/// Two separate steps, on purpose. Expiring is reversible bookkeeping: the row stays and
/// stops being served. Deleting is not, so it only touches what has been expired long
/// enough that nobody could still legitimately want to read it.
/// </para>
/// </remarks>
public sealed class NotificationRetentionService
{
    private readonly RaphaelContext _context;
    private readonly ILogger<NotificationRetentionService> _logger;

    public NotificationRetentionService(
        RaphaelContext context,
        ILogger<NotificationRetentionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationRetentionResult> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var expired = await ExpireAsync(cancellationToken);
        var deleted = await PurgeAsync(cancellationToken);

        _logger.LogInformation(
            "Notification retention: {Expired} expired, {Deleted} deleted.",
            expired,
            deleted);

        return new NotificationRetentionResult(expired, deleted);
    }

    /// <summary>
    /// Moves past-due notifications to the Expired state. Already final ones are left
    /// alone: archived or cancelled are decisions somebody made, not staleness.
    /// </summary>
    private async Task<int> ExpireAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var expirable = new[]
        {
            NotificationStatus.Created.Id,
            NotificationStatus.PendingDelivery.Id,
            NotificationStatus.Delivered.Id,
            NotificationStatus.Viewed.Id
        };

        // Notifications written before expiry existed have none. They are treated as if
        // they carried the most generous window, so the ones already in production do
        // not sit there forever waiting for a date they will never have.
        var legacyCutoff = now.Subtract(
            NotificationRetentionPolicy.LongestPurgeWindow());

        return await _context.Notifications
            .Where(n => expirable.Contains(n.StatusId)
                        && ((n.ExpiresAtUtc != null && n.ExpiresAtUtc <= now)
                            || (n.ExpiresAtUtc == null && n.CreatedAtUtc <= legacyCutoff)))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    n => n.StatusId,
                    NotificationStatus.Expired.Id),
                cancellationToken);
    }

    /// <summary>
    /// Deletes what expired long ago. Recipients, deliveries, metadata and actions go
    /// with it: the relationships cascade.
    /// </summary>
    /// <remarks>
    /// Two passes, because the window depends on who the notification was for. A notice
    /// only ever seen by staff goes after a week; anything a patient or an integration
    /// could read stays a month.
    ///
    /// <para>
    /// A notification is only ever purged on the short window when <b>every</b> one of its
    /// audiences is short lived. Otherwise deleting on the shortest would take a patient's
    /// record away because an office copy had aged out.
    /// </para>
    ///
    /// <para>
    /// Archived notifications are never deleted. Archiving is somebody deciding a record
    /// is worth keeping, and a cleanup that overrides that decision makes the state a lie.
    /// </para>
    /// </remarks>
    private async Task<int> PurgeAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var shortLivedTypeIds = NotificationRetentionPolicy.ShortLivedAudiences
            .Select(x => x.Id)
            .ToArray();

        var shortCutoff = now.Subtract(
            NotificationRetentionPolicy.ShortestPurgeWindow());

        var longCutoff = now.Subtract(
            NotificationRetentionPolicy.LongestPurgeWindow());

        //
        // Staff-only notices. The bulk of the table.
        //
        var deleted = await _context.Notifications
            .Where(n => n.StatusId != NotificationStatus.Archived.Id)
            .Where(n => n.Recipients.Any()
                        && !n.Recipients.Any(r =>
                            !shortLivedTypeIds.Contains(r.RecipientTypeId)))
            .Where(n => (n.ExpiresAtUtc ?? n.CreatedAtUtc) <= shortCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        //
        // Everything else: anything a patient or an integration could read, plus rows
        // left without an audience by older versions.
        //
        deleted += await _context.Notifications
            .Where(n => n.StatusId != NotificationStatus.Archived.Id)
            .Where(n => (n.ExpiresAtUtc ?? n.CreatedAtUtc) <= longCutoff)
            .ExecuteDeleteAsync(cancellationToken);

        return deleted;
    }
}
