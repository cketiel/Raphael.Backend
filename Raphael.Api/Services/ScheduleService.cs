using Meditrans.Shared.DbContexts;
using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Api.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly RaphaelContext _context;

        public ScheduleService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<ScheduleDto>> GetSchedulesByRunLoginAndDateAsync(string runLogin, DateTime date)
        {
            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Where(s => s.VehicleRoute.SmartphoneLogin == runLogin && s.Date == date.Date && s.Performed == false)
                //.Where(s => s.VehicleRouteId == vehicleRouteId && s.Trip.Date.Date == date.Date)
                .OrderBy(s => s.Sequence)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    TripId = s.TripId,
                    Name = s.Name,
                    Pickup = s.ScheduledPickupTime,
                    Appt = s.ScheduledApptTime,
                    Address = s.Address,
                    ScheduleLatitude = s.ScheduleLatitude,
                    ScheduleLongitude = s.ScheduleLongitude,
                    Phone = s.Phone,
                    Comment = s.Comment,
                    AuthNo = s.AuthNo,
                    FundingSource = s.FundingSourceName,
                    Driver = s.VehicleRoute.Driver.FullName,

                    ETA = s.ETATime,
                    Distance = s.DistanceToPoint,
                    Travel = s.TravelTime,
                    Arrive = s.ActualArriveTime,
                    Perform = s.ActualPerformTime,
                    ArriveDist = s.ArriveDistance,
                    PerformDist = s.PerformDistance,
                    GPSArrive = s.GpsArrive,
                    Odometer = s.Odometer,
                    Date = s.Date,
                    Sequence = s.Sequence,
                    EventType = s.EventType, // Pickup or Dropoff
                    SpaceType = s.SpaceTypeName,
                    TripType = s.Trip.Type, // (Appointment, Return)
                    Performed = s.Performed
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date)
        {
            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Where(s => s.VehicleRouteId == vehicleRouteId && s.Date == date.Date && s.Performed == false)
                //.Where(s => s.VehicleRouteId == vehicleRouteId && s.Trip.Date.Date == date.Date)
                .OrderBy(s => s.Sequence)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    TripId = s.TripId,
                    Name = s.Name,
                    Pickup = s.ScheduledPickupTime,
                    Appt = s.ScheduledApptTime,
                    Address = s.Address,
                    ScheduleLatitude = s.ScheduleLatitude,
                    ScheduleLongitude = s.ScheduleLongitude,
                    Phone = s.Phone,
                    Comment = s.Comment,
                    AuthNo = s.AuthNo,
                    FundingSource = s.FundingSourceName,
                    Driver = s.VehicleRoute.Driver.FullName, 
                                                             
                    ETA = s.ETATime,
                    Distance = s.DistanceToPoint,
                    Travel = s.TravelTime,
                    Arrive = s.ActualArriveTime,
                    Perform = s.ActualPerformTime,
                    ArriveDist = s.ArriveDistance,
                    PerformDist = s.PerformDistance,
                    GPSArrive = s.GpsArrive,
                    Odometer = s.Odometer,    
                    Date = s.Date,
                    Sequence = s.Sequence,
                    EventType = s.EventType,
                    Performed = s.Performed,
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
                    FundingSource = t.FundingSource.Name,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    Distance = t.Distance,
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
                    WillCall = t.WillCall,
                    Status = t.Status,
                    FundingSourceId = t.FundingSourceId,                
                })
                .ToListAsync();
        }

        // When the first trip is routed for a route on a specific day,
        // the system will automatically create "Pull-out" and "Pull-in" events for that day.
        // Subsequent trips for the same route and day will simply be inserted between these two events.
        public async Task RouteTripAsync(RouteTripRequest request)
        {
            // 1. Validate that the main entities exist.
            // AsNoTracking is used for the route, since we will not modify it.
            var vehicleRoute = await _context.VehicleRoutes.AsNoTracking()
                .FirstOrDefaultAsync(vr => vr.Id == request.VehicleRouteId);
            if (vehicleRoute == null)
            {
                throw new KeyNotFoundException("VehicleRoute not found.");
            }

            // We load the trip with its relationships to create the schedules.
            var tripToRoute = await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.FundingSource)
                .Include(t => t.SpaceType)
                .FirstOrDefaultAsync(t => t.Id == request.TripId);

            if (tripToRoute == null)
            {
                throw new KeyNotFoundException($"Trip with ID {request.TripId} not found.");
            }

            if (tripToRoute.VehicleRouteId.HasValue)
            {
                throw new InvalidOperationException($"Trip with ID {request.TripId} is already routed.");
            }

            // Start a transaction to ensure the atomicity of the operation.
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 2. Update the Trip entity.
                tripToRoute.VehicleRouteId = request.VehicleRouteId;
                tripToRoute.Status = TripStatus.Scheduled;

                // It is not necessary to call _context.Trips.Update(tripToRoute) because
                // Entity Framework is already tracking changes to this entity.

                // Create the status change log.
                var tripLog = new TripLog
                {
                    TripId = tripToRoute.Id,
                    Status = TripStatus.Scheduled,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay
                };
                _context.TripLogs.Add(tripLog);

                // 3. Check if there are already schedules for this route on this day.
                var tripDate = tripToRoute.Date.Date;
                bool isFirstTripOfDay = !await _context.Schedules
                    .AnyAsync(s => s.VehicleRouteId == request.VehicleRouteId && s.Trip.Date.Date == tripDate);

                if (isFirstTripOfDay)
                {
                    // If it is the first ride of the day, Pull-out and Pull-in events are created.
                    var pullOutEvent = new Schedule
                    {
                        VehicleRouteId = request.VehicleRouteId,
                        Name = "Pull-out",
                        Address = vehicleRoute.Garage,
                        ScheduleLatitude = vehicleRoute.GarageLatitude,
                        ScheduleLongitude = vehicleRoute.GarageLongitude,
                        ETATime = tripToRoute.FromTime - (TimeSpan.FromMinutes(20) + request.PickupTravelTime), // vehicleRoute.FromTime, 
                        DistanceToPoint = 0, // Always 0 for the first event
                        ScheduledPickupTime = TimeSpan.FromHours(0),
                        ScheduledApptTime = TimeSpan.FromHours(0),
                        TravelTime = TimeSpan.Zero,
                        Date = tripToRoute.Date,
                        Performed = false, // Not performed by default
                        // TripId is null by default
                    };

                    var pullInEvent = new Schedule
                    {
                        VehicleRouteId = request.VehicleRouteId,
                        Name = "Pull-in",
                        Address = vehicleRoute.Garage,
                        ScheduleLatitude = vehicleRoute.GarageLatitude,
                        ScheduleLongitude = vehicleRoute.GarageLongitude,
                        ETATime = request.DropoffETA + request.DropoffTravelTime, // vehicleRoute.ToTime, 
                        ScheduledPickupTime = TimeSpan.FromHours(23),
                        ScheduledApptTime = TimeSpan.FromHours(23),
                        Date = tripToRoute.Date,
                        Performed = false, // Not performed by default
                        // TripId is null by default
                    };

                    await _context.Schedules.AddRangeAsync(pullOutEvent, pullInEvent);
                }

                // 4. Create the two new Schedule events with the customer data.

                // Pickup Event
                var pickupSchedule = new Schedule
                {
                    TripId = tripToRoute.Id,
                    VehicleRouteId = request.VehicleRouteId,
                    EventType = ScheduleEventType.Pickup,
                    Name = $"{tripToRoute.Customer.FullName} Pickup - {tripToRoute.Type}",
                    Address = tripToRoute.PickupAddress,
                    ScheduleLatitude = tripToRoute.PickupLatitude,
                    ScheduleLongitude = tripToRoute.PickupLongitude,
                    Phone = tripToRoute.Customer.MobilePhone ?? tripToRoute.Customer.Phone,
                    Comment = tripToRoute.PickupComment,
                    FundingSourceName = tripToRoute.FundingSource?.Name ?? "N/A",
                    AuthNo = tripToRoute.Authorization,
                    SpaceTypeName = tripToRoute.SpaceType.Name,
                    ScheduledPickupTime = tripToRoute.FromTime,
                    // --- Data calculated by the client ---
                    DistanceToPoint = request.PickupDistance,
                    TravelTime = request.PickupTravelTime,
                    ETATime = request.PickupETA,
                    Date = tripToRoute.Date,
                    Performed = false, // Not performed by default
                };

                // Dropoff Event
                var dropoffSchedule = new Schedule
                {
                    TripId = tripToRoute.Id,
                    VehicleRouteId = request.VehicleRouteId,
                    EventType = ScheduleEventType.Dropoff,
                    Name = $"{tripToRoute.Customer.FullName} Dropoff - {tripToRoute.Type}",
                    Address = tripToRoute.DropoffAddress,
                    ScheduleLatitude = tripToRoute.DropoffLatitude,
                    ScheduleLongitude = tripToRoute.DropoffLongitude,
                    Phone = tripToRoute.Customer.MobilePhone ?? tripToRoute.Customer.Phone,
                    Comment = tripToRoute.DropoffComment,
                    FundingSourceName = tripToRoute.FundingSource?.Name ?? "N/A",
                    AuthNo = tripToRoute.Authorization,
                    SpaceTypeName = tripToRoute.SpaceType.Name,
                    ScheduledApptTime = tripToRoute.ToTime,
                    // --- Data calculated by the client ---
                    DistanceToPoint = request.DropoffDistance,
                    TravelTime = request.DropoffTravelTime,
                    ETATime = request.DropoffETA,
                    Date = tripToRoute.Date,
                    Performed = false, // Not performed by default
                };

                await _context.Schedules.AddRangeAsync(pickupSchedule, dropoffSchedule);
                await _context.SaveChangesAsync(); // We save so that new schedules obtain IDs.

                // 5. Recalculate the sequence of ALL schedules for this route on this day.
                await RecalculateSequenceForRouteAsync(request.VehicleRouteId, tripToRoute.Date);

                // 6. Save all changes and confirm the transaction.
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // If something fails, revert all changes.
                await transaction.RollbackAsync();
                throw; // Rethrow the exception for the controller to handle.
            }
        }

        public async Task CancelRouteForTripAsync(int scheduleId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var scheduleToCancel = await _context.Schedules.FindAsync(scheduleId);
                if (scheduleToCancel == null || !scheduleToCancel.TripId.HasValue)
                {
                    // You cannot cancel an event that does not exist or is not part of a trip (e.g. Pull-out)
                    throw new KeyNotFoundException("Schedule for a trip not found.");
                }
               
                var tripId = scheduleToCancel.TripId.Value;
                var vehicleRouteId = scheduleToCancel.VehicleRouteId;

                // 1. Find and delete all schedules associated with this trip
                var relatedSchedules = await _context.Schedules
                    .Where(s => s.TripId == tripId)
                    .ToListAsync();

                var tripDate = (await _context.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == tripId)).Date;

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

                await _context.SaveChangesAsync(); // We save so that the next query sees the changes.

                // 4. Check if there are other trips left for this route on this day.
                bool otherTripsExist = await _context.Schedules
                    .AnyAsync(s => s.VehicleRouteId == vehicleRouteId && s.Trip.Date.Date == tripDate.Date);

                if (!otherTripsExist)
                {
                    // If there are no more trips left, we also eliminate Pull-out and Pull-in.
                    var dayEvents = await _context.Schedules
                        .Where(s => s.VehicleRouteId == vehicleRouteId && !s.TripId.HasValue)
                        .ToListAsync();

                    _context.Schedules.RemoveRange(dayEvents);
                }
                else
                {
                    // If there are other trips left, we just recalculate the sequence.
                    await RecalculateSequenceForRouteAsync(vehicleRouteId, tripDate);
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

        private async Task RecalculateSequenceForRouteAsync(int vehicleRouteId, DateTime date)
        {
            var schedulesToSequence = await _context.Schedules
                .Include(s => s.Trip)
                // THIS LINE IS NOW VALID:
                // Will select:
                // 1. Schedules WITHOUT TripId (Pull-out/in) AND
                // 2. Schedules WITH TripId whose travel date matches.
                .Where(s => s.VehicleRouteId == vehicleRouteId && (!s.TripId.HasValue || s.Trip.Date.Date == date.Date))
                .OrderBy(s => s.ETATime)
                .ToListAsync();

            for (int i = 0; i < schedulesToSequence.Count; i++)
            {
                schedulesToSequence[i].Sequence = i;
            }
        }

        public async Task<bool> UpdateAsync(int id, ScheduleDto dto) 
        {
            var schedules = await _context.Schedules.FirstOrDefaultAsync(r => r.Id == id);

            if (schedules == null) return false;

            schedules.DistanceToPoint = dto.Distance;
            schedules.TravelTime = dto.Travel;
            schedules.ETATime = dto.ETA;
            schedules.Odometer = dto.Odometer;

            await _context.SaveChangesAsync();
            return true;
        }      

    }
}
