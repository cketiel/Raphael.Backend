using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Interfaces.Persistence;

public interface IBusinessEventRepository
{
    Task<BusinessEvent?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);
}