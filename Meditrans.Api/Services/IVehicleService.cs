using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;

namespace Meditrans.Api.Services
{
    public interface IVehicleService
    {
        Task<List<Vehicle>> GetAllAsync();
        Task<Vehicle?> GetByIdAsync(int id);
        Task<Vehicle> CreateAsync(VehicleDto dto);
        Task<Vehicle?> UpdateAsync(int id, VehicleDto dto);
        Task<bool> DeleteAsync(int id);
    }

}
