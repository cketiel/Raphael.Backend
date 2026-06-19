using Raphael.Shared.DTOs;
using System.Threading.Tasks;

namespace Raphael.Api.Services
{
    public interface IProviderService
    {
        Task<ProviderDto?> GetContactProviderAsync();
        Task<bool> UpdateContactProviderAsync(ProviderDto providerDto);
        Task<IEnumerable<ProviderDto>> GetAllAsync();
        Task<ProviderDto> CreateAsync(ProviderDto dto);
        Task<bool> UpdateAsync(int id, ProviderDto dto);
        Task<bool> DeleteAsync(int id);

    }
}