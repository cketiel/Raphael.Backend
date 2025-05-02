using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;

namespace Meditrans.Api.Services
{
    public interface ICapacityDetailTypeService
    {
        Task<List<CapacityDetailType>> GetAllAsync();
        Task<CapacityDetailType?> GetByIdAsync(int id);
        Task<CapacityDetailType> CreateAsync(CapacityDetailTypeDto dto);
        Task<CapacityDetailType?> UpdateAsync(int id, CapacityDetailTypeDto dto);
        Task<bool> DeleteAsync(int id);
    }

}
