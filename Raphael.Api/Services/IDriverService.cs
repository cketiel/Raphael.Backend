namespace Raphael.Api.Services
{
    public interface IDriverService
    {
        Task<bool> UpdatePushTokenAsync(int userId, string token);

        /// <summary>
        /// Forgets the device this driver was signing in from.
        /// </summary>
        /// <remarks>
        /// Called on sign out. Phones are shared between shifts: leaving the token behind
        /// sends the next driver's device the notifications of the previous one, which in
        /// this domain means trips that are not theirs.
        /// </remarks>
        Task<bool> ClearPushTokenAsync(int userId);
    }
}
