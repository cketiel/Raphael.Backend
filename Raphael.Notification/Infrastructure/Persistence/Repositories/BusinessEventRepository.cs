using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Persistence;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Entities.Notifications;

namespace Raphael.Notification.Infrastructure.Persistence.Repositories;

public class BusinessEventRepository : IBusinessEventRepository
{
    private readonly RaphaelContext _context;


    public BusinessEventRepository(
        RaphaelContext context)
    {
        _context = context;
    }


    public async Task<BusinessEvent?> GetByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.BusinessEvents
            .Include(be => be.Group)
            .FirstOrDefaultAsync(
                be =>
                    be.Code == code &&
                    be.IsActive,
                cancellationToken);
    }
}