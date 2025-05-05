using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Entities;
using Meditrans.Shared.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Api.Services
{
    public class TripService : ITripService
    {
        private readonly MediTransContext _context;

        public TripService(MediTransContext context)
        {
            _context = context;
        }

        public async Task<List<Trip>> GetAllAsync()
        {
            return await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .ToListAsync();
        }
        /*public async Task<IEnumerable<TripReadDto>> GetAllAsync()
        {
            return await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer.FullName,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType.Name,
                    //PickupNote = t.PickupNote,
                    IsCancelled = t.IsCancelled
                })
                .ToListAsync();
        }*/

        public async Task<TripReadDto?> GetByIdAsync(int id)
        {
            var t = await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (t == null) return null;

            return new TripReadDto
            {
                Id = t.Id,
                Day = t.Day,
                Date = t.Date,
                FromTime = t.FromTime,
                ToTime = t.ToTime,
                CustomerId = t.CustomerId,
                CustomerName = t.Customer.FullName,
                PickupAddress = t.PickupAddress,
                PickupLatitude = t.PickupLatitude,
                PickupLongitude = t.PickupLongitude,
                DropoffAddress = t.DropoffAddress,
                DropoffLatitude = t.DropoffLatitude,
                DropoffLongitude = t.DropoffLongitude,
                SpaceTypeId = t.SpaceTypeId,
                SpaceTypeName = t.SpaceType.Name,
                //PickupNote = t.PickupNote,
                IsCancelled = t.IsCancelled
            };
        }

        public async Task<TripReadDto> CreateAsync(TripCreateDto dto)
        {
            var trip = new Trip
            {
                Date = dto.Date,
                Day = dto.Date.DayOfWeek.ToString(),
                FromTime = dto.FromTime,
                ToTime = dto.ToTime,
                CustomerId = dto.CustomerId,
                PickupAddress = dto.PickupAddress,
                PickupLatitude = dto.PickupLatitude,
                PickupLongitude = dto.PickupLongitude,
                DropoffAddress = dto.DropoffAddress,
                DropoffLatitude = dto.DropoffLatitude,
                DropoffLongitude = dto.DropoffLongitude,
                SpaceTypeId = dto.SpaceTypeId,
                //PickupNote = dto.PickupNote,
                IsCancelled = false
            };

            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(trip.Id) ?? throw new Exception("Trip creation failed.");
        }

        public async Task<bool> UpdateAsync(int id, TripUpdateDto dto)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return false;

            trip.Date = dto.Date;
            trip.Day = dto.Date.DayOfWeek.ToString();
            trip.FromTime = dto.FromTime;
            trip.ToTime = dto.ToTime;
            trip.CustomerId = dto.CustomerId;
            trip.PickupAddress = dto.PickupAddress;
            trip.PickupLatitude = dto.PickupLatitude;
            trip.PickupLongitude = dto.PickupLongitude;
            trip.DropoffAddress = dto.DropoffAddress;
            trip.DropoffLatitude = dto.DropoffLatitude;
            trip.DropoffLongitude = dto.DropoffLongitude;
            trip.SpaceTypeId = dto.SpaceTypeId;
            //trip.PickupNote = dto.PickupNote;
            trip.IsCancelled = dto.IsCancelled;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return false;

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return true;
        }
    }

}
