using Raphael.Shared.DTOs;

namespace Raphael.Api.Services
{
    public interface IScheduleService
    {
        Task<IEnumerable<ScheduleDto>> GetSchedulesByRunLoginAndDateAsync(string runLogin, DateTime date);
        Task<IEnumerable<ScheduleDto>> GetPendingSchedulesForDriverAsync(string runLogin, DateTime date);
        Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date);

        /// <summary>
        /// The events of one trip — its pickup and its dropoff — ordered pickup first.
        /// </summary>
        /// <remarks>
        /// Unlike the by-route reader, this one does not hide the events of a cancelled
        /// trip. The caller already named the trip it wants, and the commonest reason to
        /// ask about a single trip is a notice saying it was cancelled.
        /// </remarks>
        Task<IEnumerable<ScheduleDto>> GetSchedulesByTripAsync(int tripId);
        Task<IEnumerable<UnscheduledTripDto>> GetUnscheduledTripsByDateAsync(DateTime date);
        Task RouteTripAsync(RouteTripRequest request);
        Task CancelRouteForTripAsync(int scheduleId);
        Task<bool> UpdateAsync(int id, ScheduleDto dto);

        /// <summary>
        /// Writes a whole route's new order in one transaction. Returns how many stops moved.
        /// </summary>
        Task<int> ResequenceAsync(ScheduleResequenceRequest request);
        Task<bool> PerformUpdateAsync(int id, ScheduleDto dto);
        Task<bool> SaveSignatureAsync(int scheduleId, byte[] signature);
        Task<byte[]?> GetSignatureAsync(int scheduleId);
        Task<IEnumerable<ScheduleDto>> GetFutureSchedulesForDriverAsync(string runLogin);

        /// <summary>
        /// Tomorrow's schedule for a run. Strictly the calendar day after today.
        /// </summary>
        Task<IEnumerable<ScheduleDto>> GetNextDaySchedulesForDriverAsync(string runLogin);

        Task<IEnumerable<ScheduleHistoryDto>> GetScheduleHistoryAsync(string runLogin, DateTime date);
        Task<int> GetScheduleHistoryCountAsync(string runLogin, DateTime date);

        Task<bool> UpdateContactPhoneNumberAsync(int tripId, string newPhoneNumber);
        Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataAsync(DateTime date, int? fundingSourceId);
        Task<IEnumerable<ProductionReportRowDto>> GetAviataReportDataAsync(DateTime startDate, DateTime endDate, List<int>? fundingSourceIds);
        Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataByRangeAsync2(DateTime startDate, DateTime endDate, List<int>? fundingSourceIds);
        Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataByRangeAsync(DateTime startDate, DateTime endDate, List<int>? fundingSourceIds, List<int>? vehicleRouteIds);

        Task<ScheduleDto?> GetByIdAsync(int id);

        Task<IEnumerable<ScheduleDto>> GetPatientETAsByNamePhoneAndDateAsync(string patientFullName, string phone, DateTime date);
        Task<IEnumerable<ScheduleDto>> GetPatientETAsByNameAsync(string patientFullName, DateTime date);

        Task<IEnumerable<ScheduleDto>> GetPatientETAsAsync(string? patientFullName, string? phone, DateTime? date, string? tripId);
        Task<bool> UpdateScheduleEtaAsync(int id, UpdateScheduleEtaDto dto);
    }
}

