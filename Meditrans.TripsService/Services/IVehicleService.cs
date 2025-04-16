using Meditrans.Shared.Entities;
using Meditrans.TripsService.DTOs;

namespace Meditrans.TripsService.Services
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
