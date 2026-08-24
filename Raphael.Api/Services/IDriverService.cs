namespace Raphael.Api.Services
{
    public interface IDriverService
    {
        Task<bool> UpdatePushTokenAsync(int userId, string token);
    }
}
