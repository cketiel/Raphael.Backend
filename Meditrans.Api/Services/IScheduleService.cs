using Meditrans.Shared.DTOs;

namespace Meditrans.Api.Services
{
    public interface IScheduleService
    {
        Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date);
        Task<IEnumerable<UnscheduledTripDto>> GetUnscheduledTripsByDateAsync(DateTime date);
        Task RouteTripsAsync(RouteTripRequest request);
        Task CancelRouteForTripAsync(int scheduleId);
    }
}
