using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface ITripService
    {
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
    }

}

