using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Notification.Domain.Events;
using Raphael.Shared.DbContexts;

namespace Raphael.Notification.Infrastructure.Persistence.Repositories;

public class BusinessEventDefinitionRepository : IBusinessEventDefinitionRepository
{
    private readonly RaphaelContext _context;

    public BusinessEventDefinitionRepository(
        RaphaelContext context)
    {
        _context = context;
    }

    public async Task<BusinessEventDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.BusinessEventDefinitions
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);
    }

    public async Task<BusinessEventDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.BusinessEventDefinitions
            .FirstOrDefaultAsync(
                x => x.Code == code,
                cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessEventDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.BusinessEventDefinitions
            .OrderBy(x => x.Group.SortOrder)
            .ThenBy(x => x.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<BusinessEventDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.BusinessEventDefinitions
            .Where(x => x.IsActive)
            .OrderBy(x => x.Group.SortOrder)
            .ThenBy(x => x.SortOrder)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        BusinessEventDefinition definition,
        CancellationToken cancellationToken = default)
    {
        await _context.BusinessEventDefinitions.AddAsync(
            definition,
            cancellationToken);
    }

    public Task UpdateAsync(
        BusinessEventDefinition definition,
        CancellationToken cancellationToken = default)
    {
        _context.BusinessEventDefinitions.Update(definition);

        return Task.CompletedTask;
    }

    public Task DeleteAsync(
        BusinessEventDefinition definition,
        CancellationToken cancellationToken = default)
    {
        _context.BusinessEventDefinitions.Remove(definition);

        return Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.BusinessEventDefinitions
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }
}