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

        public async Task<bool> ClearPushTokenAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;

            // Already empty is the outcome the caller asked for, not a failure.
            if (string.IsNullOrEmpty(user.PushToken)) return true;

            user.PushToken = null;
            return await _context.SaveChangesAsync() > 0;
        }
    }
}
