using Raphael.Shared.Entities;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IRunService
    {
        Task<IEnumerable<VehicleRoute>> GetAllAsync();
        Task<VehicleRoute?> GetByIdAsync(int id);
        Task<VehicleRoute> CreateAsync(VehicleRouteDto dto); // Task<VehicleRoute> CreateAsync(RunDto dto);
        Task<bool> UpdateAsync(int id, VehicleRouteDto dto);  // Task<bool> UpdateAsync(int id, RunDto dto);
        Task<bool> DeleteAsync(int id);
        Task<bool> CancelAsync(int id);
    }

}

