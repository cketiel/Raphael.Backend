using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface IRiderService
    {
        Task<RiderAuthResponse?> IdentifyAsync(RiderIdentifyRequest request);
        Task<IEnumerable<ScheduleDto>> GetMySchedulesAsync(int customerId, DateTime date);
        Task<IEnumerable<TripReadDto>> GetMyTripHistoryAsync(int customerId, DateTime startDate, DateTime endDate);
        Task<List<GpsDataDto>> GetMyActiveVehicleLocationAsync(int customerId);
        Task<bool> ActivateWillCallAsync(int tripId, int customerId, string customerName);
        Task<bool> UpdateProfileAsync(int customerId, CustomerCreateDto dto);
        Task<bool> SubmitRatingAsync(RatingCreateDto dto, int customerId);
        Task<bool> SavePushTokenAsync(int customerId, string token);
        Task<ExpoPushResult> SendTestPushAsync(int customerId, string message);
    }
}
