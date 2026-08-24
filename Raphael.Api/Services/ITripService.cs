using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using System.Diagnostics;

namespace Raphael.Api.Services
{
    public interface ITripService
    {
        Task<List<string>> UpsertPortalTripsAsync(List<PortalTripDto> dtos, int? integratorId);
        Task<int> CancelIntegrationTripsAsync(List<string> externalTripIds, int? integratorId, string? integratorName, string cancelledBy = CancelledByTypes.Integrator);
        Task<List<Trip>> GetIntegrationTripDetailsAsync(DateTime? date, List<string>? externalIds, int? integratorId);
        Task<List<string>> UpsertIntegrationTripsAsync(List<IntegrationTripDto> dtos, int? integratorId, string? integratorName);
        Task UpdateTripTypesAsync(List<TripTypeUpdateDto> updates);
        Task<List<TripReadDto>> GetAllAsync();
        Task<(List<TripReadDto> Trips, int TotalCount)> GetAllAsync(int pageNumber = 1, int pageSize = 20);
        //Task<IEnumerable<TripReadDto>> GetAllAsync();
        Task<TripReadDto?> GetByIdAsync(int id);
        Task<Trip> CreateAsync(TripCreateDto dto);
        Task<bool> UpdateAsync(int id, TripUpdateDto dto);
        Task<bool> DeleteAsync(int id);
        Task<List<TripReadDto>> GetByDateAsync(DateTime date);
        Task<List<TripReadDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<(List<TripReadDto> Trips, int TotalCount)> GetByDatePaginatedAsync(DateTime date, int pageNumber = 1, int pageSize = 20);
        Task<(List<TripReadDto> Trips, int TotalCount)> GetByDateRangePaginatedAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 20);
        Task<bool> CancelAsync(int id);
        Task<bool> CancelByDriverAsync(int id, string reason, string driverName);
        Task<bool> UncancelAsync(int id);
        Task<bool> UpdateFromDispatchAsync(int id, TripDispatchUpdateDto dto);
        Task<bool> AssignRunAsync(int id, int? vehicleRouteId);
        Task<bool> StartTripAsync(int id, TimeSpan? travel);
    }

}

