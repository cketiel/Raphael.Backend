using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;

namespace Meditrans.Api.Services
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
    }

}
