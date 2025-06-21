using Meditrans.Shared.DbContexts;
using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Api.Services
{
    // ScheduleService.cs
    public class ScheduleService : IScheduleService
    {
        private readonly MediTransContext _context;

        public ScheduleService(MediTransContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date)
        {
            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Where(s => s.VehicleRouteId == vehicleRouteId && s.Trip.Date.Date == date.Date)
                .OrderBy(s => s.Sequence)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    TripId = s.TripId,
                    Name = s.Name,
                    Pickup = s.ScheduledPickupTime,
                    Appt = s.ScheduledApptTime,
                    Address = s.Address,
                    Phone = s.Phone,
                    Comment = s.Comment,
                    AuthNo = s.AuthNo,
                    FundingSource = s.FundingSourceName,
                    Driver = s.VehicleRoute.Driver.FullName, 
                                                             
                    ETA = s.ETATime,
                    Distance = s.DistanceToPoint,
                    Travel = s.DistanceToPoint,
                    Arrive = s.ActualArriveTime,
                    Perform = s.ActualPerformTime,
                    ArriveDist = s.ArriveDistance,
                    PerformDist = s.PerformDistance,
                    GPSArrive = s.GpsArrive,
                    Odometer = s.Odometer,                 
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<UnscheduledTripDto>> GetUnscheduledTripsByDateAsync(DateTime date)
        {
            return await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.FundingSource)
                .Include(t => t.SpaceType)
                .Where(t => t.VehicleRouteId == null && !t.IsCancelled && t.Date.Date == date.Date)
                .Select(t => new UnscheduledTripDto
                {
                    Id = t.Id,
                    Date = t.Date,
                    CustomerName = t.Customer.FullName,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    PickupAddress = t.PickupAddress,
                    DropoffAddress = t.DropoffAddress,
                    SpaceType = t.SpaceType.Name,
                    FundingSource = t.FundingSource.Name
                })
                .ToListAsync();
        }

        public async Task RouteTripsAsync(RouteTripRequest request)
        {
            var vehicleRoute = await _context.VehicleRoutes
                .Include(vr => vr.Driver) 
                .FirstOrDefaultAsync(vr => vr.Id == request.VehicleRouteId);

            if (vehicleRoute == null) throw new KeyNotFoundException("VehicleRoute not found.");

            var tripsToRoute = await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.FundingSource)
                .Include(t => t.SpaceType)
                .Where(t => request.TripIds.Contains(t.Id))
                .ToListAsync();

            // Use a transaction to ensure data integrity
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var trip in tripsToRoute)
                {
                    // 1. Update Trip
                    trip.VehicleRouteId = request.VehicleRouteId;
                    trip.Status = TripStatus.Scheduled;
                    _context.Trips.Update(trip);

                    // 2. Create status change log
                    _context.TripLogs.Add(new TripLog { TripId = trip.Id, Status = TripStatus.Scheduled, Date = DateTime.UtcNow.Date, Time = DateTime.UtcNow.TimeOfDay });

                    // 3. Create Pickup event
                    var pickupSchedule = new Schedule
                    {
                        TripId = trip.Id,
                        VehicleRouteId = request.VehicleRouteId,
                        EventType = ScheduleEventType.Pickup,
                        Sequence = 0, 
                        Name = $"{trip.Customer.FullName} Pickup - {trip.Type}",
                        Address = trip.PickupAddress,
                        Phone = trip.Customer.MobilePhone ?? trip.Customer.Phone,
                        Comment = trip.PickupComment,
                        FundingSourceName = trip.FundingSource.Name,
                        AuthNo = trip.Authorization,
                        SpaceTypeName = trip.SpaceType.Name,
                        ScheduledPickupTime = trip.FromTime,
                    };

                    // 4. Create Dropoff event
                    var dropoffSchedule = new Schedule
                    {
                        TripId = trip.Id,
                        VehicleRouteId = request.VehicleRouteId,
                        EventType = ScheduleEventType.Dropoff,
                        Sequence = 1, 
                        Name = $"{trip.Customer.FullName} Dropoff - {trip.Type}",
                        Address = trip.DropoffAddress,
                        Phone = trip.Customer.MobilePhone ?? trip.Customer.Phone,
                        Comment = trip.DropoffComment,
                        FundingSourceName = trip.FundingSource.Name,
                        AuthNo = trip.Authorization,
                        SpaceTypeName = trip.SpaceType.Name,
                        ScheduledApptTime = trip.ToTime,
                    };

                    await _context.Schedules.AddRangeAsync(pickupSchedule, dropoffSchedule);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw; // Rethrow the exception for the controller to handle
            }
        }

        public async Task CancelRouteForTripAsync(int scheduleId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var scheduleToCancel = await _context.Schedules.FindAsync(scheduleId);
                if (scheduleToCancel == null) throw new KeyNotFoundException("Schedule not found.");

                var tripId = scheduleToCancel.TripId;

                // 1. Find and delete all schedules associated with this trip
                var relatedSchedules = await _context.Schedules
                    .Where(s => s.TripId == tripId)
                    .ToListAsync();

                _context.Schedules.RemoveRange(relatedSchedules);

                // 2. Update the original Trip
                var trip = await _context.Trips.FindAsync(tripId);
                if (trip != null)
                {
                    trip.VehicleRouteId = null;
                    trip.Status = TripStatus.Accepted; // Or the previous state that corresponds
                    _context.Trips.Update(trip);

                    // 3. Create status change log
                    _context.TripLogs.Add(new TripLog { TripId = trip.Id, Status = trip.Status, Date = DateTime.UtcNow.Date, Time = DateTime.UtcNow.TimeOfDay });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
