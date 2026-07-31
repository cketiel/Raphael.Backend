using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Persistence.Repositories;

public class NotificationRuleRepository : INotificationRuleRepository
{
    private readonly RaphaelContext _context;


    public NotificationRuleRepository(
        RaphaelContext context)
    {
        _context = context;
    }


    public async Task<NotificationRule?> GetActiveRuleAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRules
            .Include(r => r.Conditions)
            .Include(r => r.Recipients)
            .Include(r => r.Channels)
            .Include(r => r.Actions)
            .Include(r => r.BusinessEventDefinition)
            .FirstOrDefaultAsync(
                r =>
                    r.IsActive &&
                    r.BusinessEventDefinition.Code == businessEventCode,
                cancellationToken);
    }
}