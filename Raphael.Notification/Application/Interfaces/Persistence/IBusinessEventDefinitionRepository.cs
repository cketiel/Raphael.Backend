using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface IBusinessEventDefinitionRepository
{
    Task<BusinessEventDefinition?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<BusinessEventDefinition?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessEventDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BusinessEventDefinition>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        BusinessEventDefinition definition,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        BusinessEventDefinition definition,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        BusinessEventDefinition definition,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    //Task<int> SaveChangesAsync(
        //CancellationToken cancellationToken = default);
}