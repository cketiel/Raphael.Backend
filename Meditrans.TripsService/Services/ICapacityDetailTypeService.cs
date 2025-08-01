using Raphael.Shared.Entities;
using Raphael.TripsService.DTOs;

namespace Raphael.TripsService.Services
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

