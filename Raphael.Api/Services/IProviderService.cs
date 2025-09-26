using Raphael.Shared.DTOs;
using System.Threading.Tasks;

namespace Raphael.Api.Services
{
    public interface IProviderService
    {
        Task<ProviderDto?> GetContactProviderAsync();
        Task<bool> UpdateContactProviderAsync(ProviderDto providerDto);
    }
}