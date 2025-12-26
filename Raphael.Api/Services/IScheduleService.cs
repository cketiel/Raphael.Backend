using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IScheduleService
    {
        Task<IEnumerable<ScheduleDto>> GetSchedulesByRunLoginAndDateAsync(string runLogin, DateTime date);
        Task<IEnumerable<ScheduleDto>> GetPendingSchedulesForDriverAsync(string runLogin, DateTime date);
        Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date);
        Task<IEnumerable<UnscheduledTripDto>> GetUnscheduledTripsByDateAsync(DateTime date);
        Task RouteTripAsync(RouteTripRequest request);
        Task CancelRouteForTripAsync(int scheduleId);
        Task<bool> UpdateAsync(int id, ScheduleDto dto);

        Task<bool> SaveSignatureAsync(int scheduleId, byte[] signature);
        Task<byte[]?> GetSignatureAsync(int scheduleId);
        Task<IEnumerable<ScheduleDto>> GetFutureSchedulesForDriverAsync(string runLogin);

        Task<IEnumerable<ScheduleHistoryDto>> GetScheduleHistoryAsync(string runLogin, DateTime date);
        Task<int> GetScheduleHistoryCountAsync(string runLogin, DateTime date);

        Task<bool> UpdateContactPhoneNumberAsync(int tripId, string newPhoneNumber);
        Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataAsync(DateTime date, int? fundingSourceId);
    }
}

