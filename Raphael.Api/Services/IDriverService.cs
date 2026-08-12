namespace Raphael.Api.Services
{
    public interface IDriverService
    {
        Task<bool> UpdatePushTokenAsync(Guid userId, string token);
    }
}
