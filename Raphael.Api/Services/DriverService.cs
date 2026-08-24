using Raphael.Shared.DbContexts;

namespace Raphael.Api.Services
{
    public class DriverService : IDriverService
    {
        private readonly RaphaelContext _context;

        public DriverService(RaphaelContext context) => _context = context;

        public async Task<bool> UpdatePushTokenAsync(int userId, string token)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            user.PushToken = token;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
