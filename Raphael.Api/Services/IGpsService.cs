using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IGpsService
    {
        Task SaveGpsDataAsync(GpsDataDto gpsDataDto);
        Task<GpsDataDto?> GetLatestGpsDataAsync(int vehicleRouteId);

        /// <summary>
        /// Gets a history of GPS data points for a specific vehicle route on a given date.
        /// </summary>
        /// <param name="vehicleRouteId">The ID of the vehicle route.</param>
        /// <param name="date">The date for which to retrieve the history.</param>
        /// <returns>A list of GpsDataDto objects.</returns>
        Task<IEnumerable<GpsDataDto>> GetGpsHistoryForReportAsync(int vehicleRouteId, DateTime date);
    }
}
