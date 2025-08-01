using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface IFundingSourceBillingItemService
    {
        Task<List<FundingSourceBillingItem>> GetAllAsync();
        //Task<IEnumerable<FundingSourceBillingItemDto>> GetAllAsync();
        Task<FundingSourceBillingItemDto?> GetByIdAsync(int id);
        Task<FundingSourceBillingItemDto> CreateAsync(FundingSourceBillingItemDto dto);
        Task<bool> UpdateAsync(int id, FundingSourceBillingItemDto dto);
        Task<bool> DeleteAsync(int id);
    }
}

