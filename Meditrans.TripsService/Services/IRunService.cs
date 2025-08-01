using Raphael.Shared.Entities;
using Raphael.TripsService.DTOs;

namespace Raphael.TripsService.Services
{
    public interface IRunService
    {
        Task<IEnumerable<VehicleRoute>> GetAllAsync();
        Task<VehicleRoute?> GetByIdAsync(int id);
        Task<VehicleRoute> CreateAsync(RunDto dto);
        Task<bool> UpdateAsync(int id, RunDto dto);
        Task<bool> DeleteAsync(int id);
    }

}

