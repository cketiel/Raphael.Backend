using Microsoft.EntityFrameworkCore;
using Raphael.Notification.Application.Interfaces.Delivery;
using Raphael.Shared.DbContexts;

namespace Raphael.Notification.Infrastructure.Delivery;

public sealed class PushTokenProvider : IPushTokenProvider
{
    private readonly RaphaelContext _context;

    public PushTokenProvider(
        RaphaelContext context)
    {
        _context = context;
    }

    public async Task<string?> GetPushTokenAsync(
        int customerId,
        CancellationToken cancellationToken = default)
    {
        var customer =
            await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Id == customerId,
                    cancellationToken);

        return customer?.PushToken;
    }
}