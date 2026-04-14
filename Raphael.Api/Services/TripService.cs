using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using System.Linq;
using System.Net;

namespace Raphael.Api.Services
{
    public class TripService : ITripService
    {
        private readonly RaphaelContext _context;

        public TripService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task UpdateTripTypesAsync(List<TripTypeUpdateDto> updates)
        {
            // Opción A: EF Core tradicional (Cargar en memoria y actualizar)
            /*var ids = updates.Select(u => u.Id).ToList();
            var tripsToUpdate = await _context.Trips
                                              .Where(t => ids.Contains(t.Id))
                                              .ToListAsync();

            foreach (var trip in tripsToUpdate)
            {
                var updateData = updates.First(x => x.Id == trip.Id);
                trip.Type = updateData.Type;
            }

            await _context.SaveChangesAsync();*/

            
            // Opción B: EF Core 7+ (Más rápido, sin cargar entidades)
            foreach (var update in updates)
            {
                await _context.Trips
                    .Where(t => t.Id == update.Id)
                    .ExecuteUpdateAsync(s => s.SetProperty(t => t.Type, update.Type));
            }
            
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
            // Validate parameters
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

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

                // Optimization
                PickupCity = dto.PickupCity,    
                DropoffCity = dto.DropoffCity,

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

            trip.Day = dto.Day;
            trip.Date = dto.Date;          
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

            trip.Type = dto.Type;
            trip.Pickup = dto.Pickup;
            trip.PickupPhone = dto.PickupPhone;
            trip.PickupComment = dto.PickupComment;
            trip.Dropoff = dto.Dropoff;
            trip.DropoffPhone = dto.DropoffPhone;
            trip.DropoffComment = dto.DropoffComment;
            trip.TripId = dto.TripId;
            trip.Authorization = dto.Authorization;
            trip.Distance = dto.Distance;
            trip.ETA = dto.ETA;
            trip.WillCall = dto.WillCall;
            trip.VehicleRouteId = (dto.VehicleRouteId == 0) ? null : dto.VehicleRouteId;
            trip.DriverNoShowReason = dto.DriverNoShowReason;
            trip.FundingSourceId = dto.FundingSourceId;

            // Optimization
            trip.PickupCity = dto.PickupCity;
            trip.DropoffCity = dto.DropoffCity;

            // System-managed properties
            //trip.Status = TripStatus.Assigned;
            //trip.Created = DateTime.UtcNow;

            //trip.PickupNote = dto.PickupNote;
            //trip.IsCancelled = dto.IsCancelled; // lo comente porque estaba dando problemas, cuando se importaba por segunda vez el mismo viaje, al actualizar el viaje, siempre se mandaba 0, y si el viaje ya habia sido cancelado se producia esa cotradiccion isCancelled = false y status = cancelled, lo cual no es correcto

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

        public async Task<List<TripReadDto>> GetByDateAsync(DateTime date)
        {
            return await _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date == date.Date) // We filter by date (ignoring the time)
                .OrderBy(t => t.FromTime)
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
                    VehicleRouteId = t.VehicleRouteId ?? 0,
                    RunName = t.Run != null ? t.Run.Name : null,
                    WillCall = t.WillCall,
                    Status = t.Status,
                    DriverNoShowReason = t.DriverNoShowReason,
                    Created = t.Created,
                    FundingSourceId = t.FundingSourceId,
                    FundingSourceName = t.FundingSource != null ? t.FundingSource.Name : null,
                    PickupCity = t.PickupCity,
                    DropoffCity = t.DropoffCity,
                })
                .ToListAsync();
        }

        public async Task<List<TripReadDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            // Normalize dates (ignore time part)
            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date;

            // Validate that the range is valid
            if (normalizedStartDate > normalizedEndDate)
            {
                throw new ArgumentException("The start date cannot be greater than the end date");
            }

            return await _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date >= normalizedStartDate && t.Date.Date <= normalizedEndDate)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.FromTime)
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
                    VehicleRouteId = t.VehicleRouteId ?? 0,
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

        public async Task<(List<TripReadDto> Trips, int TotalCount)> GetByDatePaginatedAsync(DateTime date, int pageNumber = 1, int pageSize = 20)
        {
            // Validar parámetros
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var normalizedDate = date.Date;

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date == normalizedDate);

            var totalCount = await query.CountAsync();

            var trips = await query
                .OrderBy(t => t.FromTime)
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
                    VehicleRouteId = t.VehicleRouteId ?? 0,
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

        public async Task<(List<TripReadDto> Trips, int TotalCount)> GetByDateRangePaginatedAsync(DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 20)
        {
            if (pageNumber < 1)
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

            if (pageSize < 1 || pageSize > 100)
                throw new ArgumentException("Page size must be between 1 and 100", nameof(pageSize));

            var normalizedStartDate = startDate.Date;
            var normalizedEndDate = endDate.Date;

            if (normalizedStartDate > normalizedEndDate)
            {
                throw new ArgumentException("The start date cannot be greater than the end date");
            }

            var query = _context.Trips
                .AsNoTracking()
                .Include(t => t.Customer)
                .Include(t => t.SpaceType)
                .Include(t => t.Run)
                .Include(t => t.FundingSource)
                .Where(t => t.Date.Date >= normalizedStartDate && t.Date.Date <= normalizedEndDate);

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
                    VehicleRouteId = t.VehicleRouteId ?? 0,
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

        public async Task<bool> CancelAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Trip not found
            }

            // You cannot cancel a trip that has already been completed or cancelled.
            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
            {
                
                return false;
            }

            trip.Status = TripStatus.Canceled;
            trip.IsCancelled = true;
           
            // Create the status change log.
            var tripLog = new TripLog
            {
                TripId = id,
                Status = TripStatus.Canceled,
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay
            };
            _context.TripLogs.Add(tripLog);

            await _context.SaveChangesAsync();
            return true;
        }

        // Ahora se cancelan las 2 patas: A y B, es decir, el viaje principal y los relacionados del mismo cliente para el mismo día.
        public async Task<bool> CancelByDriverAsync(int id, string reason)
        {
            // 1. Buscamos el viaje principal
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false;
            }

            // Si el viaje ya está finalizado o cancelado, no hacemos nada.
            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
            {
                return false;
            }

            // 2. Buscamos todos los viajes pendientes del mismo cliente para el mismo día
            // Filtramos para que no sean el mismo viaje (t.Id != id) 
            // y que no estén ya finalizados o cancelados.
            var relatedTrips = await _context.Trips
                .Where(t => t.CustomerId == trip.CustomerId &&
                            t.Date.Date == trip.Date.Date && // Comparamos solo la parte de la fecha
                            t.Id != id &&
                            t.Status != TripStatus.Finished &&
                            t.Status != TripStatus.Canceled)
                .ToListAsync();

            // 3. Creamos una lista con todos los viajes a cancelar (el principal + los relacionados)
            var tripsToCancel = new List<Trip> { trip };
            tripsToCancel.AddRange(relatedTrips);

            // 4. Procesamos la cancelación para cada uno
            foreach (var t in tripsToCancel)
            {
                t.Status = TripStatus.Canceled;
                t.IsCancelled = true;
                t.DriverNoShowReason = reason;

                // Creamos el registro de log para cada viaje cancelado
                var tripLog = new TripLog
                {
                    TripId = t.Id,
                    Status = TripStatus.Canceled,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay,
                    // Opcional: se puede poner en las notas que fue cancelado en cascada
                    // Notes = t.Id == id ? $"Directly cancelled: {reason}" : $"Auto-cancelled due to main trip {id} cancellation."
                };
                _context.TripLogs.Add(tripLog);
            }

            // 5. Guardamos todos los cambios en una sola transacción
            await _context.SaveChangesAsync();
            return true;
        }

        // Solo la pata A
        public async Task<bool> CancelByDriverAsyncOld(int id, string reason)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; 
            }

            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled)
            {
                return false; // You cannot cancel a trip that has already been completed or cancelled.
            }

            // We assign the new status and reason
            trip.Status = TripStatus.Canceled;
            trip.IsCancelled = true;
            trip.DriverNoShowReason = reason; // <-- We save the driver's motive

            // We create the state change record
            var tripLog = new TripLog
            {
                TripId = id,
                Status = TripStatus.Canceled,
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay,
                //Notes = $"Cancelled by driver. Reason: {reason}" 
            };
            _context.TripLogs.Add(tripLog);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UncancelAsync(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Trip not found
            }

            // You can only reverse a canceled trip.
            if (trip.Status != TripStatus.Canceled)
            {
                return false; // The ride is not in the correct state for this operation
            }

            // Revert the status to 'Assigned' or the default status
            trip.Status = TripStatus.Assigned;
            trip.IsCancelled = false;

            // Record the change
            var tripLog = new TripLog
            {
                TripId = id,
                Status = TripStatus.Assigned, 
                //Comment = "Trip cancellation has been reversed.", 
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay
            };
            _context.TripLogs.Add(tripLog);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateFromDispatchAsync(int id, TripDispatchUpdateDto dto)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Not found
            }

            // Map only allowed fields from this DTO
            trip.Type = dto.Type;
            trip.FromTime = dto.FromTime;
            trip.WillCall = dto.WillCall;
            trip.PickupPhone = dto.PickupPhone;
            trip.PickupComment = dto.PickupComment;
            trip.DropoffPhone = dto.DropoffPhone;
            trip.DropoffComment = dto.DropoffComment;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRunAsync(int id, int? vehicleRouteId)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return false; // Not found
            }           

            trip.VehicleRouteId = vehicleRouteId;
            // Si se asigna a null, el estado vuelve a ser "Accepted" 
            trip.Status = vehicleRouteId.HasValue ? TripStatus.Scheduled : TripStatus.Accepted;

            await _context.SaveChangesAsync();
          
            return true;
        }

    }// end class

}

