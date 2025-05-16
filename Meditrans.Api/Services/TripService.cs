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

        /*public async Task<List<Trip>> GetAllAsync2()
        {
            return await _context.Trips
                //.Include(t => t.Customer)
                //.Include(t => t.SpaceType)
                //.Include(t => t.Run)
                .ToListAsync();
        }*/
        public async Task<List<TripReadDto>> GetAllAsync()
        {
            return await _context.Trips
                .AsNoTracking() // Better performance for read-only operations.
                .Include(t => t.Customer) 
                .Include(t => t.SpaceType) 
                .Include(t => t.Run) 
                .Include(t => t.FundingSource) 
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0, // We use 0 as default value if null
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();
        }

        public async Task<(List<TripReadDto> Trips, int TotalCount)> GetAllAsync(int pageNumber = 1, int pageSize = 20)
        {
            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource);

            var totalCount = await query.CountAsync();

            var trips = await query
                .OrderBy(t => t.Date)
                .ThenBy(t => t.FromTime)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new TripReadDto
                {
                    Id = t.Id,
                    Day = t.Day,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerId = t.CustomerId,
                    CustomerName = t.Customer != null ? t.Customer.FullName : null,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    SpaceTypeId = t.SpaceTypeId,
                    SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                    IsCancelled = t.IsCancelled,
                    Charge = t.Charge,
                    Paid = t.Paid,
                    Type = t.Type,
                    Pickup = t.Pickup,
                    PickupPhone = t.PickupPhone,
                    PickupComment = t.PickupComment,
                    Dropoff = t.Dropoff,
                    DropoffPhone = t.DropoffPhone,
                    DropoffComment = t.DropoffComment,
                    TripId = t.TripId,
                    Authorization = t.Authorization,
                    Distance = t.Distance,
                    ETA = t.ETA,
                    VehicleRouteId = t.VehicleRouteId ?? 0, // We use 0 as default value if null
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
                })
                .ToListAsync();

            return (trips, totalCount);
        }

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
                CustomerName = t.Customer != null ? t.Customer.FullName : null,
                PickupAddress = t.PickupAddress,
                PickupLatitude = t.PickupLatitude,
                PickupLongitude = t.PickupLongitude,
                DropoffAddress = t.DropoffAddress,
                DropoffLatitude = t.DropoffLatitude,
                DropoffLongitude = t.DropoffLongitude,
                SpaceTypeId = t.SpaceTypeId,
                SpaceTypeName = t.SpaceType != null ? t.SpaceType.Name : null,
                IsCancelled = t.IsCancelled,
                Charge = t.Charge,
                Paid = t.Paid,
                Type = t.Type,
                Pickup = t.Pickup,
                PickupPhone = t.PickupPhone,
                PickupComment = t.PickupComment,
                Dropoff = t.Dropoff,
                DropoffPhone = t.DropoffPhone,
                DropoffComment = t.DropoffComment,
                TripId = t.TripId,
                Authorization = t.Authorization,
                Distance = t.Distance,
                ETA = t.ETA,
                VehicleRouteId = t.VehicleRouteId ?? 0, // We use 0 as default value if null
                RunName = t.Run != null ? t.Run.Name : null,
                WillCall = t.WillCall,
                Status = t.Status,
                DriverNoShowReason = t.DriverNoShowReason,
                Created = t.Created,
                FundingSourceId = t.FundingSourceId,
                FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null
            };
        }

        public async Task<Trip> CreateAsync(TripCreateDto dto)
        {
            // Validate required relationships
            /*var customerExists = await _context.Customers.AnyAsync(c => c.Id == dto.CustomerId);
            if (!customerExists)
            {
                throw new ArgumentException("Invalid Customer ID");
            }

            var spaceTypeExists = await _context.SpaceTypes.AnyAsync(st => st.Id == dto.SpaceTypeId);
            if (!spaceTypeExists)
            {
                throw new ArgumentException("Invalid SpaceType ID");
            }*/

            // Map DTO to Entity
            var trip = new Trip
            {
                Day = dto.Day,
                Date = dto.Date,
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
                Type = dto.Type,
                Pickup = dto.Pickup,
                PickupPhone = dto.PickupPhone,
                PickupComment = dto.PickupComment,
                Dropoff = dto.Dropoff,
                DropoffPhone = dto.DropoffPhone,
                DropoffComment = dto.DropoffComment,
                TripId = dto.TripId,
                Authorization = dto.Authorization,
                Distance = dto.Distance,
                ETA = dto.ETA,
                WillCall = dto.WillCall,
                VehicleRouteId = dto.VehicleRouteId,
                DriverNoShowReason = dto.DriverNoShowReason,
                FundingSourceId = dto.FundingSourceId,

                // System-managed properties
                Status = TripStatus.Assigned,
                Created = DateTime.UtcNow,
                IsCancelled = false
            };

            // Add to context
            _context.Trips.Add(trip);

            // Save changes
            await _context.SaveChangesAsync();

            return trip;
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
