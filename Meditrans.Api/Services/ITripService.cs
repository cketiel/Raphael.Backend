using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;

namespace Meditrans.Api.Services
{
    public interface ITripService
    {
        Task<List<Trip>> GetAllAsync();
        //Task<IEnumerable<TripReadDto>> GetAllAsync();
        Task<TripReadDto?> GetByIdAsync(int id);
        Task<TripReadDto> CreateAsync(TripCreateDto dto);
        Task<bool> UpdateAsync(int id, TripUpdateDto dto);
        Task<bool> DeleteAsync(int id);
    }

}
