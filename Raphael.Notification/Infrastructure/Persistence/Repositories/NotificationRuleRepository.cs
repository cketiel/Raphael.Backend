using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Domain.Rules;
using Raphael.Shared.DbContexts;

namespace Raphael.Notification.Infrastructure.Persistence.Repositories;

public class NotificationRuleRepository : INotificationRuleRepository
{
    private readonly RaphaelContext _context;

    public NotificationRuleRepository(RaphaelContext context)
    {
        _context = context;
    }

    public async Task<NotificationRule?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Conditions)
            .Include(x => x.Recipients)
            .Include(x => x.Channels)
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<NotificationRule?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Conditions)
            .Include(x => x.Recipients)
            .Include(x => x.Channels)
            .Include(x => x.Actions)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRule>> GetByBusinessEventCodeAsync(
        string businessEventCode,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Conditions)
            .Include(x => x.Recipients)
            .Include(x => x.Channels)
            .Include(x => x.Actions)
            .Where(x =>
                x.IsActive &&
                x.BusinessEventDefinition.Code == businessEventCode)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationRule>> GetActiveRulesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRules
            .Include(x => x.BusinessEventDefinition)
            .Include(x => x.Conditions)
            .Include(x => x.Recipients)
            .Include(x => x.Channels)
            .Include(x => x.Actions)
            .Where(x => x.IsActive)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        NotificationRule rule,
        CancellationToken cancellationToken = default)
    {
        await _context.NotificationRules.AddAsync(rule, cancellationToken);
    }

    public Task UpdateAsync(
        NotificationRule rule,
        CancellationToken cancellationToken = default)
    {
        _context.NotificationRules.Update(rule);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        NotificationRule rule,
        CancellationToken cancellationToken = default)
    {
        _context.NotificationRules.Remove(rule);

        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.NotificationRules
            .AnyAsync(x => x.Id == id, cancellationToken);
    }
}