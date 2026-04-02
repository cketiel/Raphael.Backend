using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly RaphaelContext _context;

        public ScheduleService(RaphaelContext context)
        {
            _context = context;
        }
        public async Task<bool> UpdateContactPhoneNumberAsync(int tripId, string newPhoneNumber)
        {
            var trip = await _context.Trips
                                     .Include(t => t.Customer)
                                     .FirstOrDefaultAsync(t => t.Id == tripId);
          
            if (trip == null || trip.Customer == null)
            {
                return false;
            }

            // The trip pick-up phone number and the customer's main phone number are updated.
            trip.PickupPhone = newPhoneNumber;
            trip.Customer.Phone = newPhoneNumber;

            await _context.SaveChangesAsync();
            return true;
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
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    VehicleRouteId = s.VehicleRouteId,
                    Patient = s.Trip.Customer.FullName,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetPendingSchedulesForDriverAsync(string runLogin, DateTime date)
        {
            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.Trip).ThenInclude(t => t.Customer) 
                .Where(s => s.VehicleRoute.SmartphoneLogin == runLogin && s.Date == date.Date)

                // FILTER 1: Exclude already completed events (Performed)
                .Where(s => s.Performed == false)

                // FILTER 2: Exclude events from canceled trips
                .Where(s => s.Trip == null || s.Trip.Status != TripStatus.Canceled)

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
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    VehicleRouteId = s.VehicleRouteId,
                    Patient = s.Trip.Customer.FullName,
                    CustomerId = s.Trip.CustomerId,
                    CustomerPhone = s.Trip.Customer.Phone,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date)
        {
            var noCanceledEvents = await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Where(s => s.VehicleRouteId == vehicleRouteId && s.Date == date.Date)
                .Where(s => s.Trip == null || s.Trip.IsCancelled == false) // s.Trip.Status != TripStatus.Canceled
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
                    SpaceType = s.SpaceTypeName,
                    TripType = s.Trip.Type,
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    Patient = s.Trip.Customer.FullName,

                    Status = s.Trip.Status // These are not canceled
                })
                .ToListAsync();

            // Get PICKUPS from CANCELED trips
            var canceledTripPickups = await _context.Schedules
                .Include(s => s.Trip)
                .Where(s => s.VehicleRouteId == vehicleRouteId &&
                             s.Trip.IsCancelled == true && // We filter by canceled trips
                             s.EventType == ScheduleEventType.Pickup && // ONLY the pickups
                             s.Date == date.Date)
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
                    SpaceType = s.SpaceTypeName,
                    TripType = s.Trip.Type,
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    Patient = s.Trip.Customer.FullName,

                    Status = "Canceled" // We mark as canceled!
                })
                .ToListAsync();

            // Combine and sort the two lists
            var allEvents = noCanceledEvents.Concat(canceledTripPickups)
                                         .OrderBy(s => s.Sequence) //.OrderBy(s => s.ETA)
                                         .ToList();

            return allEvents;

            /*return await _context.Schedules
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
                    SpaceType = s.SpaceTypeName,
                    TripType = s.Trip.Type,
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    Patient = s.Trip.Customer.FullName,
                })
                .ToListAsync();*/
        }

        public async Task<IEnumerable<UnscheduledTripDto>> GetUnscheduledTripsByDateAsync(DateTime date)
        {
            return await _context.Trips
                .Include(t => t.Customer)
                .Include(t => t.FundingSource)
                .Include(t => t.SpaceType)
                .Where(t => t.VehicleRouteId == null && t.Date.Date == date.Date)
                //.Where(t => t.VehicleRouteId == null && !t.IsCancelled && t.Date.Date == date.Date)
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
                    DriverNoShowReason = t.DriverNoShowReason,
                    PickupCity = t.PickupCity,  
                    DropoffCity = t.DropoffCity,
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

                // Buscamos todos los eventos que tengan una secuencia >= a la que queremos insertar
                var existingEventsToShift = await _context.Schedules
                    .Where(s => s.VehicleRouteId == request.VehicleRouteId && s.Date == tripDate && s.Sequence >= request.TargetSequence)
                    .ToListAsync();

                foreach (var s in existingEventsToShift)
                {
                    s.Sequence += 2; // Abrimos 2 espacios
                }

                /*var pullIn = await _context.Schedules
    .FirstOrDefaultAsync(s => s.VehicleRouteId == request.VehicleRouteId && s.Name == "Pull-in" && s.Trip.Date.Date == tripToRoute.Date.Date);

                int newSequence = pullIn != null ? pullIn.Sequence : 100;

                // Al crear los DTOs de Pickup y Dropoff en el servidor:
                var pickupSchedule = new Schedule
                {
                    // ... otros campos ...
                    Sequence = newSequence,
                };
                var dropoffSchedule = new Schedule
                {
                    // ... otros campos ...
                    Sequence = newSequence + 1,
                };

                if (pullIn != null) pullIn.Sequence = newSequence + 2;*/

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
                    Phone = tripToRoute.PickupPhone,
                    Comment = tripToRoute.PickupComment,
                    FundingSourceName = tripToRoute.FundingSource?.Name ?? "N/A",
                    AuthNo = tripToRoute.Authorization,
                    SpaceTypeName = tripToRoute.SpaceType.Name,
                    ScheduledPickupTime = tripToRoute.FromTime,
                    Sequence = request.TargetSequence,
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
                    Phone = tripToRoute.DropoffPhone, // tripToRoute.Customer.MobilePhone ?? tripToRoute.Customer.Phone,
                    Comment = tripToRoute.DropoffComment,
                    FundingSourceName = tripToRoute.FundingSource?.Name ?? "N/A",
                    AuthNo = tripToRoute.Authorization,
                    SpaceTypeName = tripToRoute.SpaceType.Name,
                    ScheduledApptTime = tripToRoute.ToTime,
                    Sequence = request.TargetSequence + 1, // JUSTO DESPUÉS DEL PICKUP
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
                //.OrderBy(s => s.ETATime)
                .OrderBy(s => s.Name == "Pull-in") // false va primero, true (Pull-in) va al final
                .ThenBy(s => s.Name != "Pull-out") // false (Pull-out) va primero
                .ThenBy(s => s.Sequence) // Respect the manual/previous order
                //.ThenBy(s => s.ETATime) // This code snippet ensures that the "Pull-out" event must always be the first (Sequence 0) and the "Pull-in" event must always be the last, regardless of the estimated time.
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
            schedules.Sequence = dto.Sequence;
            schedules.Performed = dto.Performed;
            schedules.ActualArriveTime = dto.Arrive;
            schedules.ArriveDistance = dto.ArriveDist;
            schedules.GpsArrive = dto.GPSArrive;
            schedules.ActualPerformTime = dto.Perform;
            schedules.PerformDistance = dto.PerformDist;          

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SaveSignatureAsync(int scheduleId, byte[] signature)
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null)
            {
                return false;
            }

            schedule.PassengerSignature = signature;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<byte[]?> GetSignatureAsync(int scheduleId)
        {
            var schedule = await _context.Schedules
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == scheduleId);

            return schedule?.PassengerSignature;
        }

        public async Task<IEnumerable<ScheduleDto>> GetFutureSchedulesForDriverAsync(string runLogin)
        {
            var today = DateTime.Today; 

            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.Trip)
                
                .Where(s => s.VehicleRoute.SmartphoneLogin == runLogin)
                
                .Where(s => s.Date > today)
                
                .Where(s => s.Trip == null || s.Trip.Status != TripStatus.Canceled)
                .OrderBy(s => s.Date) 
                .ThenBy(s => s.Sequence)
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
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    VehicleRouteId = s.VehicleRouteId,
                    Patient = s.Trip.Customer.FullName,
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleHistoryDto>> GetScheduleHistoryAsync(string runLogin, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            // Get COMPLETED events
            var completedEvents = await _context.Schedules
                .Include(s => s.Trip) 
                .Where(s => s.VehicleRoute.SmartphoneLogin == runLogin &&
                             s.Performed == true &&
                             s.Date >= dayStart && s.Date < dayEnd)
                .Select(s => new ScheduleHistoryDto
                {
                    
                    Id = s.Id,
                    Name = s.Name,
                    Perform = s.ActualPerformTime,
                    ScheduledTime = s.ScheduledPickupTime,
                    Patient = s.Trip.Customer.FullName,
                    Address = s.Address,
                    EventType = s.EventType,
                    TripType = s.Trip.Type,


                    IsCanceled = false // These are not canceled
                })
                .ToListAsync();

            // Get PICKUPS from CANCELED trips
            var canceledTripPickups = await _context.Schedules
                .Include(s => s.Trip)
                .Where(s => s.VehicleRoute.SmartphoneLogin == runLogin &&
                             s.Trip.IsCancelled == true && // We filter by canceled trips
                             s.EventType == ScheduleEventType.Pickup && // ONLY the pickups
                             s.Date >= dayStart && s.Date < dayEnd)
                .Select(s => new ScheduleHistoryDto
                {                  
                    Id = s.Id,
                    Name = s.Name,
                    Perform = null, // It was not done
                    ScheduledTime = s.ScheduledPickupTime, // We use the scheduled time
                    Patient = s.Trip.Customer.FullName,
                    Address = s.Address,
                    EventType = s.EventType,
                    TripType = s.Trip.Type,
                    IsCanceled = true // We mark as canceled!
                })
                .ToListAsync();

            // Combine and sort the two lists
            var history = completedEvents.Concat(canceledTripPickups)
                                         .OrderBy(s => s.IsCanceled ? s.ScheduledTime : s.Perform)
                                         .ToList();

            return history;
        }
      
        public async Task<int> GetScheduleHistoryCountAsync(string runLogin, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var completedCount = await _context.Schedules
                .CountAsync(s => s.VehicleRoute.SmartphoneLogin == runLogin && s.Performed == true && s.Date >= dayStart && s.Date < dayEnd);

            var canceledCount = await _context.Schedules
                .CountAsync(s => s.VehicleRoute.SmartphoneLogin == runLogin && s.Trip.IsCancelled == true && s.EventType == ScheduleEventType.Pickup && s.Date >= dayStart && s.Date < dayEnd);

            return completedCount + canceledCount;
        }

        public async Task<IEnumerable<ProductionReportRowDto>> GetAviataReportDataAsync(DateTime startDate, DateTime endDate)
        {
            var baseQuery = _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.Trip).ThenInclude(t => t.FundingSource)
                .Include(s => s.Trip).ThenInclude(t => t.SpaceType)
                .Include(s => s.VehicleRoute)
                .Where(s => s.Date.HasValue && s.Date.Value.Date >= startDate.Date && s.Date.Value.Date <= endDate.Date && s.TripId != null);

            var schedulesForPeriod = await baseQuery.ToListAsync();

            var allBillingRules = await _context.FundingSourceBillingItems
                .Include(bi => bi.BillingItem)
                .ToListAsync();

            var reportData = schedulesForPeriod
                .GroupBy(s => s.TripId)
                .Select(tripGroup =>
                {
                    var pickup = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Pickup);
                    var dropoff = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Dropoff);
                    var trip = pickup?.Trip ?? dropoff?.Trip;

                    if (trip == null) return null;

                    var row = new ProductionReportRowDto
                    {
                        Date = trip.Date,
                        Patient = trip.Customer?.FullName,
                        DOB = trip.Customer?.DOB,
                        PatientAddress = $"{trip.Customer?.Address}, {trip.Customer?.City}, {trip.Customer?.State} {trip.Customer?.Zip}",
                        FundingSource = trip.FundingSource?.Name,
                        PickupAddress = trip.PickupAddress,
                        DropoffAddress = trip.DropoffAddress,
                        Run = pickup?.VehicleRoute?.Name ?? dropoff?.VehicleRoute?.Name,
                        Distance = trip.Distance,
                        Canceled = trip.IsCancelled,
                        TripId = trip.TripId,
                        Authorization = trip.Authorization,
                        BillableLines = new List<ChargeLineDto>()
                    };

                    var rules = allBillingRules.Where(r => r.FundingSourceId == trip.FundingSourceId && r.SpaceTypeId == trip.SpaceTypeId).ToList();

                    if (trip.IsCancelled)
                    {
                        row.BillableLines.Add(new ChargeLineDto { ChargeName = "CANCELATION FEE", Quantity = 1.0, Rate = 35.0 });
                    }
                    else
                    {
                        // PICK UP FEE
                        var loadRule = rules.FirstOrDefault(r => r.BillingItem.Description.Contains("Loading") || r.BillingItem.Description.Contains("PICK UP"));
                        if (loadRule != null)
                        {
                            row.BillableLines.Add(new ChargeLineDto
                            {
                                ChargeName = "PICK UP FEE",
                                Quantity = 1.0,
                                Rate = (double)loadRule.Rate
                            });
                        }

                        // MILES 
                        var milesRule = rules.FirstOrDefault(r => r.BillingItem.Description.Contains("MILES"));
                        if (milesRule != null)
                        {
                            double tripMiles = trip.Distance ?? 0.0;
                            double freeQty = (double)(milesRule.FreeQty ?? 0);

                            // Usamos Math.Max con doubles explícitos
                            double billableMiles = Math.Max(0.0, tripMiles - freeQty);

                            row.BillableLines.Add(new ChargeLineDto
                            {
                                ChargeName = "MILES",
                                Quantity = billableMiles,
                                Rate = (double)milesRule.Rate
                            });
                        }
                    }

                    return row;
                })
                .Where(row => row != null)
                .OrderBy(row => row.Patient)
                .ThenBy(row => row.Date)
                .ToList();

            return reportData;
        }
        public async Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataAsync(DateTime date, int? fundingSourceId)
        {
            var baseQuery = _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.Trip).ThenInclude(t => t.FundingSource)
                .Include(s => s.Trip).ThenInclude(t => t.SpaceType)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Where(s => s.Date.HasValue && s.Date.Value.Date == date.Date && s.TripId != null);

            if (fundingSourceId.HasValue)
            {
                baseQuery = baseQuery.Where(s => s.Trip.FundingSourceId == fundingSourceId.Value);
            }

            var schedulesForDay = await baseQuery.ToListAsync();

            // Obtener TODAS las reglas de cobro para los FundingSources y SpaceTypes involucrados en este día
            var fsIds = schedulesForDay.Select(s => s.Trip.FundingSourceId).Distinct().ToList();
            var stIds = schedulesForDay.Select(s => s.Trip.SpaceTypeId).Distinct().ToList();

            var allBillingRules = await _context.FundingSourceBillingItems
                .Include(bi => bi.BillingItem)
                .Where(bi => fsIds.Contains(bi.FundingSourceId) && stIds.Contains(bi.SpaceTypeId))
                .ToListAsync();

            var reportData = schedulesForDay
                .GroupBy(s => s.TripId)
                .Select(tripGroup =>
                {
                    var pickup = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Pickup);
                    var dropoff = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Dropoff);
                    var trip = pickup?.Trip ?? dropoff?.Trip;

                    if (trip == null) return null;

                    // --- LÓGICA DE CÁLCULO DE FACTURACIÓN ---
                    decimal totalBilled = 0;
                    double distance = trip.Distance ?? 0;

                    // Filtrar las reglas que aplican a ESTE viaje específico
                    var currentRules = allBillingRules
                        .Where(r => r.FundingSourceId == trip.FundingSourceId && r.SpaceTypeId == trip.SpaceTypeId)
                        .ToList();

                    // A. Loading Fee (Costo fijo)
                    var loadingFeeItem = currentRules.FirstOrDefault(r =>
                        r.BillingItem.Description.Contains("Loading Fee", StringComparison.OrdinalIgnoreCase));
                    if (loadingFeeItem != null)
                    {
                        totalBilled += loadingFeeItem.Rate;
                    }

                    // B. MILES (Cálculo por distancia con FreeQty)
                    var milesItem = currentRules.FirstOrDefault(r =>
                        r.BillingItem.Description.Contains("MILES", StringComparison.OrdinalIgnoreCase));
                    if (milesItem != null)
                    {
                        int freeMiles = milesItem.FreeQty ?? 0;
                        double billableMiles = Math.Max(0, distance - freeMiles);
                        totalBilled += (decimal)billableMiles * milesItem.Rate;
                    }

                    return new ProductionReportRowDto
                    {
                        Date = trip.Date,
                        ReqPickup = trip.FromTime,
                        Appointment = trip.ToTime,
                        Patient = trip.Customer?.FullName,
                        PickupAddress = trip.PickupAddress,
                        DropoffAddress = trip.DropoffAddress,
                        Space = trip.SpaceType?.Name,
                        //Charge = trip.Charge,
                        Charge = (double)totalBilled,
                        Paid = trip.Paid,
                        PickupComment = trip.PickupComment,
                        DropoffComment = trip.DropoffComment,
                        Type = trip.Type,
                        PickupPhone = trip.PickupPhone,
                        DropoffPhone = trip.DropoffPhone,
                        Authorization = trip.Authorization,
                        FundingSource = trip.FundingSource?.Name,
                        Distance = trip.Distance,
                        Run = pickup?.VehicleRoute?.Name ?? dropoff?.VehicleRoute?.Name,
                        Driver = pickup?.VehicleRoute?.Driver?.FullName,
                        PickupArrive = pickup?.ActualArriveTime,
                        PickupPerform = pickup?.ActualPerformTime,
                        DropoffArrive = dropoff?.ActualArriveTime,
                        DropoffPerform = dropoff?.ActualPerformTime,
                        WillCall = trip.WillCall,
                        Canceled = trip.IsCancelled,
                        VIN = pickup?.VehicleRoute?.Vehicle?.VIN,
                        PickupOdometer = pickup?.Odometer,
                        DropoffOdometer = dropoff?.Odometer,
                        WillCallTime = trip.WillCall ? trip.FromTime : null, // null, //
                        Vehicle = pickup?.VehicleRoute?.Vehicle?.Name,
                        VehiclePlate = pickup?.VehicleRoute?.Vehicle?.Plate,
                        TripId = trip.TripId,
                        PickupGpsArriveDistance = pickup?.ArriveDistance,
                        DropoffGpsArriveDistance = dropoff?.ArriveDistance,
                        PickupCity = trip.PickupCity,
                        PickupState = trip.Customer?.State, //
                        PickupZip = trip.Customer?.Zip,
                        DropoffCity = trip.DropoffCity,
                        DropoffState = trip.Customer?.State,
                        DropoffZip = trip.Customer?.Zip,
                        PatientAddress = trip.Customer?.Address,
                        DOB = trip.Customer?.DOB,
                        DriverNoShowReason = trip.DriverNoShowReason,
                        PickupLat = trip.PickupLatitude,
                        PickupLon = trip.PickupLongitude,
                        DropoffLat = trip.DropoffLatitude,
                        DropoffLon = trip.DropoffLongitude,
                        Created = trip.Created,
                        PickupSignature = pickup?.PassengerSignature,
                    };
                })
                .Where(row => row != null)
                .OrderBy(row => row.Run)
                .ThenBy(row => row.ReqPickup)
                .ToList();

            return reportData;
        }

        public async Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataAsyncOld(DateTime date, int? fundingSourceId)
        {
            // Fetch all relevant schedules for the given date.
            // We include related entities needed for the report.
            /*var schedulesForDay = await _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.VehicleRoute)             
                .Where(s => s.Date.HasValue && s.Date.Value.Date == date.Date && s.TripId != null)
                .ToListAsync();*/

            // Start with the base query
            var baseQuery = _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.VehicleRoute)
                .Where(s => s.Date.HasValue && s.Date.Value.Date == date.Date && s.TripId != null);

            // --- NEW: Conditionally apply the FundingSource filter ---
            if (fundingSourceId.HasValue)
            {
                // Add a WHERE clause to filter by the FundingSourceId on the related Trip
                baseQuery = baseQuery.Where(s => s.Trip.FundingSourceId == fundingSourceId.Value);
            }

            // Now, execute the final query
            var schedulesForDay = await baseQuery.ToListAsync();

            // Group the schedules by TripId. Each group should contain a Pickup and a Dropoff event.
            var reportData = schedulesForDay
                .GroupBy(s => s.TripId)
                .Select(tripGroup =>
                {
                    var pickupSchedule = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Pickup);
                    var dropoffSchedule = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Dropoff);

                    if (pickupSchedule == null || dropoffSchedule == null || !pickupSchedule.Date.HasValue)
                    {
                        return null; // Filter out incomplete trips or those with no date.
                    }

                    // Project the data from the two schedules into a single DTO.
                    return new ProductionReportRowDto
                    {                       
                        Date = pickupSchedule.Date.Value.Date,
                        Authorization = pickupSchedule.AuthNo,
                        ReqPickup = pickupSchedule.ScheduledPickupTime,
                        Appointment = dropoffSchedule.ScheduledApptTime,
                        Patient = pickupSchedule.Trip?.Customer?.FullName,
                        PickupCity = pickupSchedule.Trip?.PickupCity,
                        Run = pickupSchedule.VehicleRoute?.Name,
                        Space = pickupSchedule.SpaceTypeName,
                        PickupArrive = pickupSchedule.ActualArriveTime,
                        DropoffArrive = dropoffSchedule.ActualArriveTime
                    };
                })
                .Where(row => row != null)
                .OrderBy(row => row.Run)
                .ThenBy(row => row.ReqPickup)
                .ToList();

            return reportData;
        }

        public async Task<ScheduleDto?> GetByIdAsync(int id)
        {
            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Where(s => s.Id == id)
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
                    SpaceType = s.SpaceTypeName,
                    TripType = s.Trip.Type,
                    Performed = s.Performed,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    VehicleRouteId = s.VehicleRouteId,
                    Patient = s.Trip.Customer.FullName,
                    CustomerId = s.Trip.CustomerId,
                    CustomerPhone = s.Trip.Customer.Phone,
                    Status = s.Trip.Status.ToString()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetPatientETAsByNamePhoneAndDateAsync(string patientFullName, string phone, DateTime date)
        {
            // Search for the specific date provided by the user
            DateTime searchDate = date.Date;

            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Where(s =>
                    // Match Name (Partial)
                    s.Trip.Customer.FullName.ToLower().Contains(patientFullName.ToLower())
                    // Match Phone or Mobile Phone (Exact match against input)
                    && (s.Trip.Customer.Phone == phone || s.Trip.Customer.MobilePhone == phone)
                    // Match Date
                    && s.Date.Value.Date == searchDate)
                // Order by Performed status first (Upcoming first) then by ETA
                .OrderBy(s => s.Performed)
                .ThenBy(s => s.ETATime)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    TripId = s.TripId,
                    Name = s.Name,
                    Pickup = s.ScheduledPickupTime,
                    Appt = s.ScheduledApptTime,
                    Address = s.Address,
                    Driver = s.VehicleRoute.Driver.FullName,
                    ETA = s.ETATime,
                    Perform = s.ActualPerformTime, // Important to show when it was finished
                    Date = s.Date,
                    EventType = s.EventType,
                    Patient = s.Trip.Customer.FullName,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    Status = s.Trip.Status.ToString(),
                    Performed = s.Performed // Return this so the JS can split the lists
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetPatientETAsByNameAsync(string patientFullName, DateTime date)
        {
            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Where(s => s.Trip.Customer.FullName.ToLower().Contains(patientFullName.ToLower())
                            && s.Date.Value.Date == date.Date
                            && !s.Performed)
                .OrderBy(s => s.ETATime)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    TripId = s.TripId,
                    Name = s.Name,
                    Pickup = s.ScheduledPickupTime,
                    Appt = s.ScheduledApptTime,
                    Address = s.Address,
                    Driver = s.VehicleRoute.Driver.FullName,
                    ETA = s.ETATime,
                    Date = s.Date,
                    EventType = s.EventType,
                    Patient = s.Trip.Customer.FullName,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    Status = s.Trip.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<ScheduleDto>> GetPatientETAsAsync(string? patientFullName, string? phone, DateTime? date, string? tripId)
        {
            var query = _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tripId))
            {
                // Search strictly by TripId (identifier from Broker/Funding Source)
                query = query.Where(s => s.Trip.TripId == tripId.Trim());
            }
            else
            {
                // Search by Profile (Name + Phone + Date)
                string pName = patientFullName.ToLower().Trim();
                query = query.Where(s =>
                    s.Trip.Customer.FullName.ToLower().Contains(pName) &&
                    (s.Trip.Customer.Phone == phone || s.Trip.Customer.MobilePhone == phone) &&
                    s.Date.Value.Date == date.Value.Date
                );
            }

            return await query
                .OrderBy(s => s.Performed) // Show non-completed first
                .ThenBy(s => s.ETATime)
                .Select(s => new ScheduleDto
                {
                    Id = s.Id,
                    TripId = s.TripId,
                    Name = s.Name,
                    Pickup = s.ScheduledPickupTime,
                    Appt = s.ScheduledApptTime,
                    Address = s.Address,
                    Driver = s.VehicleRoute.Driver.FullName,
                    ETA = s.ETATime,
                    Perform = s.ActualPerformTime,
                    Date = s.Date,
                    EventType = s.EventType,
                    Patient = s.Trip.Customer.FullName,
                    Run = s.VehicleRoute.Name,
                    Vehicle = s.VehicleRoute.Vehicle.Name,
                    Status = s.Trip.Status.ToString(),
                    Performed = s.Performed
                })
                .ToListAsync();
        }

    }
}

