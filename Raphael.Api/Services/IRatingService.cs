using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public interface IRatingService
    {
        Task<RatingReadDto?> GetByIdAsync(int id);
        Task<List<RatingReadDto>> GetByDriverIdAsync(int driverId);
        Task<RatingReadDto> CreateAsync(RatingCreateDto dto);
        Task<bool> UpdateAsync(int id, double newScore, string? newComment);
        Task<bool> DeleteAsync(int id);
    }
}