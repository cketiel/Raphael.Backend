using Raphael.TripsService.DTOs;

namespace Raphael.TripsService.Services
{
    public interface ITripService
    {
        Task<IEnumerable<TripReadDto>> GetAllAsync();
        Task<TripReadDto?> GetByIdAsync(int id);
        Task<TripReadDto> CreateAsync(TripCreateDto dto);
        Task<bool> UpdateAsync(int id, TripUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

}

