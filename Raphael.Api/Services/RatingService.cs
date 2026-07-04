using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public class RatingService : IRatingService
    {
        private readonly RaphaelContext _context;

        public RatingService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<RatingReadDto?> GetByIdAsync(int id)
        {         
            var rating = await _context.Ratings
                .Include(r => r.Customer)
                .Include(r => r.Driver)
                .FirstOrDefaultAsync(r => r.Id == id);
         
            if (rating == null) return null;
          
            return MapToDto(rating);
        }

        public async Task<List<RatingReadDto>> GetByDriverIdAsync(int driverId)
        {
            return await _context.Ratings
                .Where(r => r.DriverId == driverId)
                .Include(r => r.Customer)
                .Select(r => MapToDto(r))
                .ToListAsync();
        }

        public async Task<RatingReadDto> CreateAsync(RatingCreateDto dto)
        {
            var rating = new Rating
            {
                TripId = dto.TripId,
                CustomerId = dto.CustomerId,
                DriverId = dto.DriverId,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();

            // Load names for the return DTO
            await _context.Entry(rating).Reference(r => r.Customer).LoadAsync();
            await _context.Entry(rating).Reference(r => r.Driver).LoadAsync();

            return MapToDto(rating);
        }

        public async Task<bool> UpdateAsync(int id, double newScore, string? newComment)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null) return false;

            rating.Score = newScore;
            rating.Comment = newComment;
            rating.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rating = await _context.Ratings.FindAsync(id);
            if (rating == null) return false;

            _context.Ratings.Remove(rating);
            await _context.SaveChangesAsync();
            return true;
        }

        private static RatingReadDto MapToDto(Rating r) => new RatingReadDto
        {
            Id = r.Id,
            TripId = r.TripId,
            CustomerId = r.CustomerId,
            CustomerName = r.Customer?.FullName ?? "Unknown",
            DriverId = r.DriverId,
            DriverName = r.Driver?.FullName ?? "Unknown",
            Score = r.Score,
            Comment = r.Comment,
            CreatedAt = r.CreatedAt
        };
    }
}