using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface IVehicleTypeService
    {
        Task<List<VehicleType>> GetAllAsync();
        Task<VehicleType?> GetByIdAsync(int id);
        Task<VehicleType> CreateAsync(VehicleType dto);
        Task<VehicleType?> UpdateAsync(int id, VehicleType dto);
        Task<bool> DeleteAsync(int id);
    }
}

