using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface IIntegratorService
    {
        Task<IEnumerable<IntegratorDto>> GetAllAsync();
        Task<IntegratorDto?> GetByIdAsync(int id);
        Task<IntegratorDto> CreateAsync(IntegratorDto dto);
        Task<bool> UpdateAsync(int id, IntegratorDto dto);
        Task<bool> DeleteAsync(int id);
        Task<FundingSource?> GetFundingSourceByIntegratorIdAsync(int? integratorId);
    }
}