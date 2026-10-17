using Raphael.Shared.Entities;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IRunService
    {
        Task<IEnumerable<VehicleRoute>> GetAllAsync();
        Task<VehicleRoute?> GetByIdAsync(int id);
        Task<VehicleRoute> CreateAsync(VehicleRouteDto dto); // Task<VehicleRoute> CreateAsync(RunDto dto);
        /// <summary>
        /// Saves the route. Null when it does not exist. A new garage is carried onto every planned
        /// day and those days are re-chained, in the same transaction (RE-052); the answer says what
        /// that did.
        /// </summary>
        /// <param name="confirmedFingerprint">
        /// The preview the user confirmed, when there was one: the save is refused untouched if the
        /// route moved since.
        /// </param>
        /// <param name="ct">
        /// Stops the save before its commit, and nothing is kept. The commit itself is the point of no
        /// return and is never cancelled halfway.
        /// </param>
        Task<GarageChangePreviewDto?> UpdateAsync(int id, VehicleRouteDto dto, string? confirmedFingerprint = null, CancellationToken ct = default);
        Task<bool> DeleteAsync(int id);
        Task<bool> CancelAsync(int id);
        Task<VehicleRoute?> GetActiveRunByDriverIdAsync(int driverId);
    }

}

