using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Application.Services;

public class BusinessEventService
{
    private readonly IBusinessEventRepository _businessEventRepository;


    public BusinessEventService(
        IBusinessEventRepository businessEventRepository)
    {
        _businessEventRepository = businessEventRepository;
    }


    public async Task<BusinessEvent?> GetAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _businessEventRepository.GetByCodeAsync(
            code,
            cancellationToken);
    }
}