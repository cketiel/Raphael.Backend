using Microsoft.EntityFrameworkCore;
using Raphael.Api.Realtime;
using Raphael.Api.Services.Notifications;
using Raphael.Api.Services.Routing;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Interfaces.Events;
using Raphael.Notification.Application.Services;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;

using Raphael.Api.Services.Billing;

namespace Raphael.Api.Services
{
    public class ScheduleService : IScheduleService
    {
        private readonly RaphaelContext _context;
        private readonly NotificationService _notificationService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITripNotificationPublisher _tripNotifications;
        private readonly IOperationClock _clock;
        private readonly IObservedLegRecorder _observedLegs;

        private readonly IDispatchBroadcaster _board;

        public ScheduleService(RaphaelContext context, NotificationService notificationService, ICurrentUserService currentUserService, ITripNotificationPublisher tripNotifications, IOperationClock clock, IObservedLegRecorder observedLegs, IDispatchBroadcaster board)
        {
            _context = context;
            _notificationService = notificationService;
            _currentUserService = currentUserService;
            _tripNotifications = tripNotifications;
            _clock = clock;
            _observedLegs = observedLegs;
            _board = board;
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
                    Wait = s.EarlyArrivalWait,
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
                .Where(s => s.Trip == null || s.Trip.IsCancelled != true)
                //.Where(s => s.Trip == null || s.Trip.Status != TripStatus.Canceled)

                .OrderBy(s => s.Sequence)
                .ThenBy(s => s.ETATime) // This secondary order ensures that if there are any events with the same sequence (e.g. manual entries), they will be ordered by their estimated time.
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
                    Wait = s.EarlyArrivalWait,
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

        /// <summary>
        /// One route's events for one day, in the order the driver will perform them.
        /// </summary>
        /// <remarks>
        /// This is the query behind the top grid of the dispatcher's Schedule tab, and it used
        /// to be two round trips whose results were concatenated and sorted in memory: one for
        /// the live events, one to bring back the Pickup of each cancelled trip so the row stays
        /// visible in red. Both sets come from one pass now, and SQL Server does the ordering.
        ///
        /// A cancelled trip still contributes its Pickup and only its Pickup: the row is there
        /// to tell the dispatcher a hole opened in the route, and two rows would say it twice.
        /// </remarks>
        public async Task<IEnumerable<ScheduleDto>> GetSchedulesByRouteAndDateAsync(int vehicleRouteId, DateTime date)
        {
            var day = date.Date;

            return await _context.Schedules
                .AsNoTracking()
                .Where(s => s.VehicleRouteId == vehicleRouteId && s.Date == day)
                .Where(s => s.Trip == null
                            || s.Trip.IsCancelled == false
                            || s.EventType == ScheduleEventType.Pickup)
                .OrderBy(s => s.Sequence).ThenBy(s => s.ETATime)
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
                    Wait = s.EarlyArrivalWait,
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

                    // A cancelled trip's row says so, whatever the trip's own status field
                    // reads: the two markers are allowed to disagree and the dispatcher must
                    // see the cancellation.
                    Status = s.Trip.IsCancelled ? "Canceled" : s.Trip.Status
                })
                .ToListAsync();

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
                    Wait = s.EarlyArrivalWait,
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

        /// <summary>
        /// The events of one trip — its pickup and its dropoff — ordered pickup first.
        /// </summary>
        /// <remarks>
        /// Callers that want the leg of a single trip used to ask for the whole day of its
        /// line and keep the two rows they needed. That reads dozens of rows to use two,
        /// misses a trip whose events were filed under a different date, and returns
        /// nothing at all for a trip nobody has routed.
        ///
        /// <para>
        /// ⚠️ Cancelled trips are NOT filtered out here, unlike
        /// <see cref="GetSchedulesByRouteAndDateAsync"/>. The caller named this trip; the
        /// commonest reason to ask about one is a notice saying it was cancelled, and
        /// hiding its events would leave that notice with nothing to show.
        /// </para>
        /// </remarks>
        public async Task<IEnumerable<ScheduleDto>> GetSchedulesByTripAsync(int tripId)
        {
            return await _context.Schedules
                .Where(s => s.TripId == tripId)
                .OrderBy(s => s.EventType)
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
                    Wait = s.EarlyArrivalWait,
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
                    Status = s.Trip.Status
                })
                .ToListAsync();
        }

        /// <summary>
        /// The trips of one day that have no route yet — the backlog the dispatcher works from.
        /// </summary>
        /// <remarks>
        /// Cancelled trips are excluded here rather than shipped out and dropped by the client.
        /// A cancelled trip is not an offer to route anything, so sending three or four hundred
        /// rows for the client to sieve was paying twice: once on the wire, once in the grid.
        /// A trip cancelled while a dispatcher already has the tab open is a different matter,
        /// and that one arrives over the hub.
        /// </remarks>
        public async Task<IEnumerable<UnscheduledTripDto>> GetUnscheduledTripsByDateAsync(DateTime date)
        {
            // Half-open range instead of t.Date.Date == date.Date. The second form wraps the
            // column in CONVERT(date, …), which stops SQL Server seeking any index on Date and
            // leaves it scanning the whole table for one operating day.
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var trips = await _context.Trips
                .AsNoTracking()
                .Where(t => t.VehicleRouteId == null
                            && !t.IsCancelled
                            && t.Date >= dayStart && t.Date < dayEnd)
                .OrderBy(t => t.FromTime)
                .Select(t => new UnscheduledTripDto
                {
                    Id = t.Id,
                    Date = t.Date,
                    CustomerName = t.Customer.FullName,
                    CustomerPhone = t.Customer.Phone,
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
                    IsCanceled = t.IsCancelled,
                    ProviderId = t.ProviderId,
                })
                .ToListAsync();

            // Resolved here and not in the projection: the fallback chain is code, not
            // something the database can answer. Desktop gets the effective zone, so the
            // hour it suggests for a Will Call is the hour at the pickup address.
            foreach (var trip in trips)
                trip.ProviderTimeZoneId = _clock.ZoneFor(trip.ProviderId).Id;

            return trips;
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

            // A cancelled trip cannot be put on a route. Without this, routing one would
            // set Status to Scheduled below and quietly uncancel it while IsCancelled
            // stayed true: the two markers would disagree, UncancelAsync would refuse it
            // from then on, a driver would be handed a patient who was told not to expect
            // a vehicle, and that patient would be sent TRIP_SCHEDULED.
            //
            // Both markers are checked, not just one, because trips already exist with
            // them out of step — this same hole is what put them there.
            if (tripToRoute.IsCancelled || tripToRoute.Status == TripStatus.Canceled)
            {
                throw new InvalidOperationException(
                    $"Trip with ID {request.TripId} is cancelled and cannot be routed.");
            }

            // Every clock field on a Schedule is a SQL `time`: a time of day, 00:00:00 to
            // 23:59:59.9999999. The hours below are built by adding and subtracting
            // TimeSpans, which has no floor at midnight and no ceiling at 24h, so a route
            // that runs late — or one bad leg from Google — produces a value the column
            // cannot hold. Saving it failed with a raw SqlDbType.Time overflow that named
            // no field, and the dispatcher got a bare 400.
            //
            // The trip's own hours are refused, not trimmed. Trimming an ETA of 25:30 down
            // to 23:59 lets the routing succeed and hands the driver a believable wrong
            // hour: nobody finds out until a patient is not collected. A refusal that names
            // the field says which input is broken, and the trip stays unrouted until it is.
            // The garage hours are trimmed instead — see ClampToGarageHour below.
            static void RequireTimeOfDay(string field, TimeSpan value)
            {
                if (value >= TimeSpan.Zero && value < TimeSpan.FromDays(1)) return;

                throw new InvalidOperationException(
                    $"{field} is {value:g}, which is not a time of day: it must be between " +
                    "00:00:00 and 23:59:59. Check the trip's hours and its pickup and dropoff " +
                    "coordinates, then route it again.");
            }

            RequireTimeOfDay("The pickup ETA", request.PickupETA);
            RequireTimeOfDay("The travel time to the pickup", request.PickupTravelTime);
            RequireTimeOfDay("The dropoff ETA", request.DropoffETA);
            RequireTimeOfDay("The travel time to the dropoff", request.DropoffTravelTime);
            RequireTimeOfDay("The travel time back to the garage", request.ReturnToGarageTravelTime);

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
                var nextDay = tripDate.AddDays(1);

                // Half-open range: s.Date.Value.Date wraps the column in CONVERT(date, ...) and
                // the index on (VehicleRouteId, Date) cannot be seeked through it. This runs on
                // every trip routed.
                bool isFirstTripOfDay = !await _context.Schedules
                    .AnyAsync(s => s.VehicleRouteId == request.VehicleRouteId
                                   && s.Date >= tripDate && s.Date < nextDay);

                if (isFirstTripOfDay)
                {
                    // If it is the first ride of the day, Pull-out and Pull-in events are created.
                    //
                    // Pull-out and Pull-in are the vehicle leaving the garage and coming back
                    // to it. No patient is waiting on either hour, so when the arithmetic runs
                    // off the ends of the day they are pinned to the ends of the day rather
                    // than refusing the routing: an early start becomes 00:00:00 and a late
                    // return becomes 23:59:59. The dispatcher's recalculation overwrites both
                    // with measured hours immediately afterwards.
                    static TimeSpan? ClampToGarageHour(TimeSpan? value)
                    {
                        if (value is null) return null;
                        if (value.Value < TimeSpan.Zero) return TimeSpan.Zero;

                        var endOfDay = new TimeSpan(23, 59, 59);
                        return value.Value > endOfDay ? endOfDay : value.Value;
                    }

                    var pullOutEta = ClampToGarageHour(
                        tripToRoute.FromTime - (TimeSpan.FromMinutes(20) + request.PickupTravelTime));

                    // The return leg, not the trip's own leg. This used to add
                    // request.DropoffTravelTime — the pickup-to-dropoff duration — which
                    // charged the drive back to the garage the length of the trip that had
                    // just ended. See RouteTripRequest.ReturnToGarageTravelTime.
                    var pullInEta = ClampToGarageHour(
                        request.DropoffETA + request.ReturnToGarageTravelTime);

                    var pullOutEvent = new Schedule
                    {
                        VehicleRouteId = request.VehicleRouteId,
                        Name = "Pull-out",
                        Address = vehicleRoute.Garage,
                        ScheduleLatitude = vehicleRoute.GarageLatitude,
                        ScheduleLongitude = vehicleRoute.GarageLongitude,
                        ETATime = pullOutEta, // vehicleRoute.FromTime, 
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
                        ETATime = pullInEta, // vehicleRoute.ToTime, 
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
                    Phone = tripToRoute.PickupPhone ?? tripToRoute.Customer.Phone,
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
                    Phone = tripToRoute.DropoffPhone ?? tripToRoute.Customer.Phone, // tripToRoute.Customer.MobilePhone ?? tripToRoute.Customer.Phone,
                    Comment = tripToRoute.DropoffComment,
                    FundingSourceName = tripToRoute.FundingSource?.Name ?? "N/A",
                    AuthNo = tripToRoute.Authorization,
                    SpaceTypeName = tripToRoute.SpaceType.Name,
                    ScheduledApptTime = tripToRoute.ToTime,
                    Sequence = request.TargetSequence + 1, // JUSTO DESPU�S DEL PICKUP
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

                // 5b. And the garage hour, now that we know which stop is first. Inserting a
                // trip ahead of the whole route moves the departure, and until this line the
                // only thing that ever moved it was the dispatch client — which is why routing
                // a six o'clock patient onto a route that started at seven left the driver with
                // a Pull-out an hour too late.
                await ApplyDerivedRouteValuesAsync(request.VehicleRouteId, tripToRoute.Date);

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

            // Outside the try: the transaction is already committed, so a failure here
            // would try to roll back what is done and report an error for a trip that
            // was in fact routed.
            await _tripNotifications.TripScheduledAsync(tripToRoute);

            // Signal to Raphael.Driver that the schedule it has on screen is out of date.
            // Only reaches a driver already out of the garage; the publisher checks that.
            await _tripNotifications.DriverRouteUpdatedAsync(
                tripToRoute,
                RouteChangeTypes.Added);

            // And to the other dispatchers: the trip they still have in their backlog is taken.
            // Not a notification — nobody has to read this, it just stops a second dispatcher
            // from routing a trip that is already on a vehicle.
            await _board.TripRoutedAsync(
                tripToRoute.Id,
                request.VehicleRouteId,
                tripToRoute.Date,
                tripToRoute.ProviderId);
        }

        public async Task CancelRouteForTripAsync(int scheduleId)
        {
            // Captured for the signal below: the driver of the route the trip is leaving has
            // to be told, and by the time this returns the trip no longer points at it.
            Trip? unroutedTrip = null;
            int? formerVehicleRouteId = null;

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

                // 4. Whether other trips are left for this route on this day is no longer asked.
                // The answer fed the block below, which is commented out because Pull-out and
                // Pull-in must survive an empty route — so the count was a round trip whose
                // result nobody read, and it scanned the route's whole history to get it.
                /*bool otherTripsExist = await _context.Schedules
                    .AnyAsync(s => s.VehicleRouteId == vehicleRouteId && s.Trip.Date.Date == tripDate.Date);*/

                // Nunca eliminar los Pull-out/in, aunque no queden viajes, pq si no hay conexion con el servidor entonces las app clientes eliminan los pull-out/in y se desconfigura toda la ruta. Lo que si se puede hacer es recalcular la secuencia para que Pull-out sea 0 y Pull-in el �ltimo, aunque no queden viajes entre medio.
                /*if (!otherTripsExist)
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
                }*/

                await RecalculateSequenceForRouteAsync(vehicleRouteId, tripDate);

                // Taking the first trip off a route moves the garage hour just as much as
                // putting one in front of it does.
                await ApplyDerivedRouteValuesAsync(vehicleRouteId, tripDate);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                unroutedTrip = trip;
                formerVehicleRouteId = vehicleRouteId;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Outside the try, for the same reason as in RouteTripAsync: the transaction is
            // committed, and a failure here must not roll back a trip that really was taken
            // off the route.
            if (unroutedTrip is not null)
            {
                await _tripNotifications.DriverRouteUpdatedAsync(
                    unroutedTrip,
                    RouteChangeTypes.Removed,
                    formerVehicleRouteId);

                // The trip is waiting again: it has to reappear in the other dispatchers'
                // backlog, and the route it left has a hole in it that they should see.
                await _board.TripUnroutedAsync(
                    unroutedTrip.Id,
                    formerVehicleRouteId ?? 0,
                    unroutedTrip.Date,
                    unroutedTrip.ProviderId);
            }
        }

        /// <summary>
        /// Renumbers one route's stops for one day, Pull-out first and Pull-in last.
        /// </summary>
        /// <remarks>
        /// ⚠️ The day is decided by the schedule's own Date, not by its trip's.
        ///
        /// It used to read <c>!s.TripId.HasValue || s.Trip.Date.Date == date.Date</c>, and
        /// Pull-out and Pull-in carry no TripId — so the first branch matched them on **every
        /// date the route has ever run**. Routing the first trip of tomorrow renumbered today's
        /// garage events, which in business terms means reordering the route of a driver who may
        /// be out on the road. It also grew without limit: two more rows per route per operating
        /// day, materialised on every routing and every cancellation, for ever.
        ///
        /// The range is half-open rather than a .Date comparison on both sides: schedules are
        /// written with the trip's Date unnormalised, so a row that carries a time of day would
        /// otherwise disappear from its own route.
        /// </remarks>
        private async Task RecalculateSequenceForRouteAsync(int vehicleRouteId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            var schedulesToSequence = await _context.Schedules
                .Where(s => s.VehicleRouteId == vehicleRouteId
                            && s.Date >= dayStart && s.Date < dayEnd)
                //.OrderBy(s => s.ETATime)
                .OrderBy(s => s.Name == "Pull-in") // false va primero, true (Pull-in) va al final
                .ThenBy(s => s.Name != "Pull-out") // false (Pull-out) va primero
                .ThenBy(s => s.Sequence) // Respect the manual/previous order
                //.ThenBy(s => s.ETATime) // This code snippet ensures that the "Pull-out" event must always be the first (Sequence 0) and the "Pull-in" event must always be the last, regardless of the estimated time.
                .ToListAsync();

            for (int i = 0; i < schedulesToSequence.Count; i++)
            {
                schedulesToSequence[i].Sequence = i;
                if (schedulesToSequence[i].Name == "Pull-out")
                    {
                    schedulesToSequence[i].Sequence = 0; // Ensure Pull-out is always 0
                }
                else if (schedulesToSequence[i].Name == "Pull-in")
                {
                    schedulesToSequence[i].Sequence = schedulesToSequence.Count - 1; // Ensure Pull-in is always last
                }
            }

        }

        /// <summary>How long the vehicle needs at the garage before it can leave.</summary>
        private const int PullOutPreparationMinutes = 20;

        /// <summary>How long a vehicle stands at a stop serving it.</summary>
        /// <remarks>
        /// ⚠️ <c>Schedule</c> has no per-stop service time, and <c>ScheduleDto.On</c> — the column
        /// the dispatcher sees — is filled by no projection in this file, so it is always null and
        /// both sides fall back to this number. If that column is ever populated, this constant
        /// and <c>SchedulesViewModel.PlannedDepartureFrom</c> have to learn about it together.
        /// </remarks>
        private const int StopServiceMinutes = 15;

        /// <summary>
        /// The hour the vehicle is planned to leave a stop for the next one.
        /// </summary>
        /// <remarks>
        /// The hour it really happened if it has, the estimate if it has not, plus the time spent
        /// there. Nothing is spent at the garage: nobody is served at a Pull-out.
        ///
        /// <para>
        /// ⚠️ Mirrors <c>SchedulesViewModel.PlannedDepartureFrom</c> in Raphael.Desktop, and has
        /// to keep mirroring it. The two chain the same route and must agree on where each leg
        /// starts, or the hour the dispatcher reads and the wait the server stores describe
        /// different journeys.
        /// </para>
        /// </remarks>
        private static TimeSpan PlannedDepartureFrom(Schedule stop)
        {
            if (stop is null) return TimeSpan.Zero;

            // ⚠️ The perform time only counts while the stop is still marked performed. A
            // dispatcher can undo a performed stop — see PerformUpdateAsync and the un-perform
            // action on the schedule grid — and the hour stays in the column while the flag goes.
            // SchedulesViewModel.DepartureTimeOf tests the flag, so this must too, or the two
            // chain a route that was un-performed from different starting hours.
            var arrival = (stop.Performed ? stop.ActualPerformTime : null)
                          ?? stop.ActualArriveTime
                          ?? stop.ETATime
                          ?? TimeSpan.Zero;

            var service = stop.Name == "Pull-out"
                ? TimeSpan.Zero
                : TimeSpan.FromMinutes(StopServiceMinutes);

            return arrival + service;
        }

        /// <summary>
        /// Derives what the route works out for itself: the garage departure hour, and how long
        /// each pickup leaves its driver waiting.
        /// </summary>
        /// <remarks>
        /// ⚠️ The Pull-out hour is derived data, and until this method existed nothing on the
        /// server derived it: <see cref="RouteTripAsync"/> wrote it once, when the day's first
        /// trip was routed, and every later change of shape trusted whatever the dispatch
        /// client worked out and sent back. Raphael.Desktop has several paths that never send
        /// it — reassigning a run from the Trips tab is one — and one that computes the hour,
        /// paints it, and then decides it has nothing to save. The garage hour was then
        /// describing a route that no longer exists: the driver is told to leave at seven for
        /// a patient collected at six.
        ///
        /// <para>
        /// Call it after anything that changes a route's shape. Clients may keep computing and
        /// sending their own value; this runs afterwards and has the last word, so the hour is
        /// right whether or not any client got round to asking.
        /// </para>
        ///
        /// <para>
        /// Does not save: the callers are inside transactions of their own and the row is
        /// tracked. Returns whether it changed anything.
        /// </para>
        /// </remarks>
        private async Task<bool> ApplyDerivedRouteValuesAsync(int vehicleRouteId, DateTime date)
        {
            var dayStart = date.Date;
            var dayEnd = dayStart.AddDays(1);

            // Whether the trip behind each row is cancelled, asked of the database rather than
            // walked through a navigation property: the rows are wanted for their own sake and
            // loading a Trip each to read one flag is a join per stop.
            var rows = await _context.Schedules
                .Where(s => s.VehicleRouteId == vehicleRouteId
                            && s.Date >= dayStart && s.Date < dayEnd)
                .Select(s => new
                {
                    Stop = s,
                    TripCancelled = s.Trip != null && s.Trip.IsCancelled,
                    TripType = s.Trip != null ? s.Trip.Type : null
                })
                .ToListAsync();

            // ⚠️ Ordered here and not in the query. Every caller renumbers these same rows just
            // before calling, and those new numbers are only in the change tracker — the
            // database would sort by the sequence they had a moment ago and hand back the old
            // first stop.
            var ordered = rows.OrderBy(r => r.Stop.Sequence ?? int.MaxValue).ToList();

            var changed = ApplyEarlyArrivalWaits(
                ordered.Select(r => (r.Stop, r.TripCancelled, r.TripType)).ToList());

            var pullOut = ordered.FirstOrDefault(r => r.Stop.Name == "Pull-out")?.Stop;
            if (pullOut is null) return changed;

            // Once the driver has left, the hour is history and ActualPerformTime holds it.
            // Moving the estimate now rewrites, on the driver's own screen, a departure that
            // already happened.
            if (pullOut.Performed) return changed;

            var firstStop = ordered.FirstOrDefault(r =>
                    r.Stop.TripId.HasValue
                    && r.Stop.Name != "Pull-in"
                    // A cancellation nobody drove to is not a stop: it sets no hour for the
                    // garage. One the driver did reach is, and it stays in the chain.
                    && (!r.TripCancelled || r.Stop.ActualArriveTime.HasValue))
                ?.Stop;

            // An empty route keeps the hour it has. Pull-out and Pull-in survive a route with
            // no trips left on purpose — see CancelRouteForTripAsync — and inventing a garage
            // hour with nothing to derive it from is worse than leaving yesterday's.
            if (firstStop is null) return changed;

            var commitment = firstStop.ScheduledPickupTime ?? firstStop.ScheduledApptTime;
            if (commitment is null) return changed;

            var hour = ClampToDayEnds(
                commitment.Value
                - TimeSpan.FromMinutes(PullOutPreparationMinutes)
                - (firstStop.TravelTime ?? TimeSpan.Zero));

            if (pullOut.ETATime == hour) return changed;

            pullOut.ETATime = hour;
            return true;
        }

        /// <summary>How close to a promised hour a vehicle may be shown arriving.</summary>
        /// <remarks>
        /// Five minutes on a return leg: the patient has finished at the clinic and is waiting to
        /// be collected, so holding the vehicle a quarter of an hour down the road buys nobody
        /// anything. ⚠️ Mirrors the margins in <c>SchedulesViewModel.ChainEtas</c> and in
        /// <c>Raphael.Driver</c>'s <c>ApplyEarlyArrivalLimit</c>; all three have to agree.
        /// </remarks>
        private static TimeSpan EarlyArrivalMarginFor(string? tripType)
        {
            return tripType == "Return" ? TimeSpan.FromMinutes(5) : TimeSpan.FromMinutes(15);
        }

        /// <summary>A time of day with the seconds cut off, not rounded.</summary>
        private static TimeSpan ToWholeMinutes(TimeSpan value)
        {
            return TimeSpan.FromMinutes(Math.Floor(value.TotalMinutes));
        }

        /// <summary>
        /// Works out, for every pickup on the route, how long the driver stands there waiting for
        /// the hour to come round.
        /// </summary>
        /// <remarks>
        /// The route never shows a vehicle arriving more than a quarter of an hour before the
        /// hour the patient was promised. What is not shortened is the drive — the roads take
        /// what they take — so when that rule raises an arrival, the difference is the driver
        /// sitting in a vehicle outside a house with the engine off.
        ///
        /// <para>
        /// ⚠️ The rule is tested for, not inferred by subtraction. This used to work out the wait
        /// as <c>ETA − (previous departure + drive)</c> and report whatever came out, which
        /// assumed the stored chain was consistent with itself. After a reorder it is not: the
        /// client sends only the rows it changed, a leg nobody could price keeps the drive it had
        /// against a neighbour it no longer has, and that subtraction then produced a number out
        /// of the drift and called it waiting. A trip arriving two hours <em>late</em> was
        /// reported as waiting an hour and eleven minutes — the row painted amber for arriving
        /// early and red for arriving late at the same time, over a tooltip explaining a rule
        /// that had never fired.
        /// </para>
        ///
        /// <para>
        /// So the test is the rule itself. Raising an arrival lands it exactly on
        /// <c>promised − margin</c>; an arrival anywhere else was not raised, and a stop with no
        /// promised hour, or one reached at or after it, cannot have been. Where the stored chain
        /// disagrees with itself the answer is no wait rather than an invented one.
        /// </para>
        ///
        /// <para>
        /// Dropoffs are excluded because the rule does not apply to them: the patient is already
        /// in the vehicle and arriving early is simply arriving early. So are the garage events,
        /// whose scheduled hour is a sentinel rather than a promise to anybody.
        /// </para>
        ///
        /// <para>
        /// Once the driver has arrived, the plan stops being the story: what really happened is
        /// in <c>ActualArriveTime</c> and <c>ActualPerformTime</c>. The figure is frozen at
        /// whatever it last was rather than recomputed against hours that have since moved.
        /// </para>
        /// </remarks>
        /// <param name="ordered">The route's stops, already in running order, each with whether
        /// the trip behind it is cancelled and what kind of trip it is.</param>
        /// <returns>Whether any row changed.</returns>
        private static bool ApplyEarlyArrivalWaits(
            List<(Schedule Stop, bool TripCancelled, string? TripType)> ordered)
        {
            var changed = false;
            Schedule? previous = null;

            foreach (var (stop, tripCancelled, tripType) in ordered)
            {
                // ⚠️ The previous *physical* stop, which is what the chain is built from. A
                // cancellation nobody drove to is not one: no vehicle ever went there, so it sets
                // no departure hour for the stop after it. One the driver did reach is a real
                // stop and stays in the chain.
                //
                // Mirrors SchedulesViewModel.FindValidPrevious, and has to: if the two disagree
                // about which stop a leg starts from, the hour on the dispatcher's screen and the
                // wait in this column are measuring different journeys.
                var isPhysicalStop = !tripCancelled || stop.ActualArriveTime.HasValue;

                if (stop.EventType != ScheduleEventType.Pickup)
                {
                    if (isPhysicalStop) previous = stop;
                    continue;
                }

                var wait = WaitAt(stop, previous, tripType);

                if (stop.EarlyArrivalWait != wait)
                {
                    stop.EarlyArrivalWait = wait;
                    changed = true;
                }

                if (isPhysicalStop) previous = stop;
            }

            return changed;
        }

        /// <summary>
        /// How long one pickup leaves its driver waiting, or null when the early-arrival rule did
        /// not put them there.
        /// </summary>
        private static TimeSpan? WaitAt(Schedule stop, Schedule? previous, string? tripType)
        {
            // Already there: what happened is recorded, and the plan is not the story any more.
            if (stop.ActualArriveTime.HasValue) return stop.EarlyArrivalWait;

            if (previous is null) return null;
            if (stop.ETATime is not { } eta) return null;
            if (stop.ScheduledPickupTime is not { } promised) return null;

            eta = ToWholeMinutes(eta);
            promised = ToWholeMinutes(promised);

            // Nobody waits past the hour they were promised. A vehicle arriving at or after it
            // is late or on time, and either way the rule never touched this arrival.
            if (eta >= promised) return null;

            // And when the rule does touch it, it puts the arrival exactly here. An arrival
            // anywhere else got there on its own.
            if (eta != promised - EarlyArrivalMarginFor(tripType)) return null;

            var earliestArrival = ToWholeMinutes(
                PlannedDepartureFrom(previous) + (stop.TravelTime ?? TimeSpan.Zero));

            var gap = eta - earliestArrival;

            return gap > TimeSpan.Zero ? gap : null;
        }

        // Este nuevo metodo actualiza el estado de los viajes
        public async Task<bool> PerformUpdateAsync(int id, ScheduleDto dto)
        {
            // 1. Cargamos el Schedule. 
            // Usamos Include solo si realmente necesitamos actualizar el Trip
            var schedule = await _context.Schedules
                .Include(s => s.Trip)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (schedule == null) return false;

            // Noted before the mapping overwrites it. Only the transition is worth measuring: a
            // driver whose app retries the same confirmation must not have the leg counted twice,
            // or the average for that hour drifts towards whoever has the worst signal.
            bool justArrived = !schedule.ActualArriveTime.HasValue && dto.Arrive.HasValue;

            // 2. Mapeo de campos b�sicos
            schedule.DistanceToPoint = dto.Distance;
            schedule.TravelTime = dto.Travel;
            schedule.ETATime = dto.ETA;
            schedule.Odometer = dto.Odometer;
            schedule.Sequence = dto.Sequence;
            schedule.Performed = dto.Performed;
            schedule.ActualArriveTime = dto.Arrive;
            schedule.ArriveDistance = dto.ArriveDist;
            schedule.GpsArrive = dto.GPSArrive;
            schedule.ActualPerformTime = dto.Perform;
            schedule.PerformDistance = dto.PerformDist;

            if (schedule.Name == "Pull-out")
                schedule.Sequence = 0;

            // 3. L�gica de Historial y Status
            // Verificamos si el Trip existe
            if (schedule.Trip != null)
            {
                string newStatus = schedule.EventType == ScheduleEventType.Pickup
                                   ? TripStatus.InProgress
                                   : (schedule.EventType == ScheduleEventType.Dropoff ? TripStatus.Finished : null);

                if (!string.IsNullOrEmpty(newStatus))
                {
                    schedule.Trip.Status = newStatus;

                    // UTC, like the other eleven places that write a TripLog. This one was
                    // the odd one out on the server's own clock, which made the column mean
                    // two different things depending on which code path filled it.
                    //
                    // TripLog is an instant, not a pickup time: nothing displays it today,
                    // and whatever does will convert. Splitting the column by writer is the
                    // failure mode, not the choice of zone.
                    var performedAtUtc = DateTime.UtcNow;

                    // Creamos el log pero con un Try-Catch interno o verificando nulos
                    var historyLog = new TripLog
                    {
                        TripId = schedule.Trip.Id,
                        Status = newStatus,
                        Date = performedAtUtc.Date,
                        Time = performedAtUtc.TimeOfDay
                    };

                    _context.TripLogs.Add(historyLog);
                }
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Esto te dir� en los logs del servidor qu� columna fall� exactamente
                var msg = ex.InnerException?.Message ?? ex.Message;
                throw new Exception($"Error en SaveChanges: {msg}");
            }

            // What the drive actually took, now that the arrival is saved. This is the ecosystem
            // learning from its own vehicles instead of buying the same estimate again.
            if (justArrived)
            {
                await _observedLegs.RecordArrivalAsync(schedule);
            }

            // Published after the save. Performing the pickup means the patient is in the
            // vehicle, and performing the dropoff means the trip is done: two facts the
            // dispatch office plans on, so they must be true before anybody hears them.
            if (schedule.Trip != null)
            {
                if (schedule.EventType == ScheduleEventType.Pickup)
                {
                    await _tripNotifications.DriverPickedUpPassengerAsync(schedule.Trip);
                }
                else if (schedule.EventType == ScheduleEventType.Dropoff)
                {
                    await _tripNotifications.DriverCompletedTripAsync(schedule.Trip);
                }
            }

            return true;
        }

        public async Task<bool> UpdateAsync(int id, ScheduleDto dto)
        {
            // Pull-out and Pull-in are the vehicle leaving the garage and coming back
            // to it. No patient is waiting on either hour, so when the arithmetic runs
            // off the ends of the day they are pinned to the ends of the day rather
            // than refusing the routing: an early start becomes 00:00:00 and a late
            // return becomes 23:59:59. The dispatcher's recalculation overwrites both
            // with measured hours immediately afterwards.
            static TimeSpan? ClampToGarageHour(TimeSpan? value)
            {
                if (value is null) return null;
                if (value.Value < TimeSpan.Zero) return TimeSpan.Zero;

                var endOfDay = new TimeSpan(23, 59, 59);
                return value.Value > endOfDay ? endOfDay : value.Value;
            }

            var schedules = await _context.Schedules
                .Include(s => s.Trip)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (schedules == null) return false;

            // The driver pressing Arrive is the only signal the system gets that the
            // vehicle reached the pickup address, and it comes through this generic save.
            // Detecting the transition here avoids changing the contract with an app that
            // is distributed by sideload.
            bool justArrivedAtPickup =
                schedules.EventType == ScheduleEventType.Pickup
                && !schedules.ActualArriveTime.HasValue
                && dto.Arrive.HasValue;

            // The same transition without the event-type test: a dropoff is just as much a
            // measured drive as a pickup, and the router will need both.
            bool justArrived = !schedules.ActualArriveTime.HasValue && dto.Arrive.HasValue;

            var validatedEta = ClampToGarageHour(dto.ETA);

            schedules.DistanceToPoint = dto.Distance;
            schedules.TravelTime = dto.Travel;
            schedules.ETATime = validatedEta; // dto.ETA;
            schedules.Odometer = dto.Odometer;
            schedules.Sequence = dto.Sequence;
            schedules.Performed = dto.Performed;
            schedules.ActualArriveTime = dto.Arrive;
            schedules.ArriveDistance = dto.ArriveDist;
            schedules.GpsArrive = dto.GPSArrive;
            schedules.ActualPerformTime = dto.Perform;
            schedules.PerformDistance = dto.PerformDist;

            if(schedules.Name == "Pull-out")
                schedules.Sequence = 0;

            if (justArrivedAtPickup && schedules.Trip != null &&
                schedules.Trip.Status == TripStatus.Started)
            {
                // Arrived, not Waiting: Waiting means the patient rang and nobody is on
                // the way yet, which is the opposite situation for a dispatcher.
                schedules.Trip.Status = TripStatus.Arrived;

                _context.TripLogs.Add(new TripLog
                {
                    TripId = schedules.Trip.Id,
                    Status = TripStatus.Arrived,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay
                });
            }

            await _context.SaveChangesAsync();

            if (justArrived)
            {
                await _observedLegs.RecordArrivalAsync(schedules);
            }

            if (justArrivedAtPickup && schedules.Trip != null)
            {
                await _tripNotifications.DriverArrivedPickupAsync(schedules.Trip);
            }

            return true;
        }

        /// <summary>
        /// Writes a whole route's new order in one go.
        /// </summary>
        /// <remarks>
        /// Deliberately narrower than <see cref="UpdateAsync"/>. Reordering a route says nothing
        /// about whether a driver arrived anywhere, so this touches only the four fields the
        /// router recalculates and never trips the arrival detection, the observed-leg recorder
        /// or the notifications that hang off that method. Dragging a stop cannot, from here,
        /// tell a patient their driver has arrived.
        ///
        /// Stops are matched against the route and the day they claim to belong to; anything
        /// else in the request is ignored rather than trusted.
        /// </remarks>
        public async Task<int> ResequenceAsync(ScheduleResequenceRequest request)
        {
            if (request?.Stops is null || request.Stops.Count == 0) return 0;

            var dayStart = request.Date.Date;
            var dayEnd = dayStart.AddDays(1);
            var ids = request.Stops.Select(s => s.Id).ToList();

            var schedules = await _context.Schedules
                .Where(s => ids.Contains(s.Id)
                            && s.VehicleRouteId == request.VehicleRouteId
                            && s.Date >= dayStart && s.Date < dayEnd)
                .ToListAsync();

            if (schedules.Count == 0) return 0;

            var wanted = request.Stops.ToDictionary(s => s.Id);
            var changed = 0;

            foreach (var schedule in schedules)
            {
                if (!wanted.TryGetValue(schedule.Id, out var stop)) continue;

                // Same clamp as the single-row save: the garage events have no patient waiting
                // on them, so an hour that ran off the end of the day is pinned to the end of
                // the day rather than refusing the whole reorder.
                var eta = ClampToDayEnds(stop.ETA);
                var sequence = schedule.Name == "Pull-out" ? 0 : stop.Sequence;

                if (schedule.Sequence == sequence
                    && schedule.ETATime == eta
                    && schedule.TravelTime == stop.Travel
                    && schedule.DistanceToPoint == stop.Distance)
                {
                    continue;
                }

                schedule.Sequence = sequence;
                schedule.ETATime = eta;
                schedule.TravelTime = stop.Travel;
                schedule.DistanceToPoint = stop.Distance;
                changed++;
            }

            // After the client's own numbers, and with the last word. A reorder is exactly when
            // the garage hour moves, and the client is not required to have noticed: it may be
            // sending only the rows it thinks changed, and one of the paths that computes the
            // hour on screen never adds the Pull-out row to the batch at all.
            if (await ApplyDerivedRouteValuesAsync(request.VehicleRouteId, request.Date))
            {
                changed++;
            }

            if (changed > 0)
            {
                await _context.SaveChangesAsync();

                // Anyone else with this route open is looking at the order it had a moment ago.
                // The message says which route moved and not how: the receiving screen reloads
                // that one route, which is a single query, and may be showing it under a
                // different filter than the sender was.
                await _board.RouteChangedAsync(request.VehicleRouteId, request.Date);
            }

            return changed;
        }

        /// <summary>
        /// Pins an hour that ran off either end of the day to that end of the day.
        /// </summary>
        private static TimeSpan? ClampToDayEnds(TimeSpan? value)
        {
            if (value is null) return null;
            if (value.Value < TimeSpan.Zero) return TimeSpan.Zero;

            var endOfDay = new TimeSpan(23, 59, 59);
            return value.Value > endOfDay ? endOfDay : value.Value;
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
            // The driver's today, where they are driving. Taken from the server's clock, a
            // host west of the operation still calls it yesterday through the last hours of
            // the evening, and the driver opens their app to a schedule that already ran.
            //
            // The broker's zone, because a query that spans a driver's whole day has no one
            // trip to take a provider from. A driver working for a provider in another zone
            // would need this widened — noted in _meta/BACKLOG.md.
            var today = _clock.TodayFor(null);

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
                    Wait = s.EarlyArrivalWait,
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

        /// <summary>
        /// Tomorrow's schedule for a driver's run. Tomorrow only, never further.
        /// </summary>
        /// <remarks>
        /// <see cref="GetFutureSchedulesForDriverAsync"/> returns every day ahead, which is
        /// what its name says and what other callers may still want. It is not what a driver
        /// can use: a run planned for the week came back as one list with several Pull-outs
        /// and several Pull-ins in it, and the driver had no way to tell which day a row
        /// belonged to. What they need before finishing a shift is the next day's work.
        ///
        /// <para>
        /// Strictly the calendar day after today. If tomorrow is empty the answer is empty,
        /// even when there is work the day after: showing Thursday under a heading the driver
        /// reads as "tomorrow" is how somebody drives to a pickup two days early.
        /// </para>
        ///
        /// <para>
        /// Today comes from the business clock, not from the machine: a host west of the
        /// operation still calls it yesterday through the last hours of the evening, which is
        /// exactly when a driver looks at tomorrow.
        /// </para>
        /// </remarks>
        public async Task<IEnumerable<ScheduleDto>> GetNextDaySchedulesForDriverAsync(string runLogin)
        {
            var tomorrow = _clock.TodayFor(null).AddDays(1);
            var dayAfter = tomorrow.AddDays(1);

            return await _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.Trip)

                .Where(s => s.VehicleRoute.SmartphoneLogin == runLogin)

                // A half-open range rather than an equality: Schedule.Date is a DateTime, and
                // a row saved with a time on it would never match the midnight of a day.
                .Where(s => s.Date >= tomorrow && s.Date < dayAfter)

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
                    Wait = s.EarlyArrivalWait,
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

                    // The two the driver can act on tomorrow: calling and texting the patient
                    // are the only things this screen offers. CustomerPhone is the number
                    // dispatch keeps on the customer record, and the one the app dials.
                    CustomerId = s.Trip.CustomerId,
                    CustomerPhone = s.Trip.Customer.Phone,
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

        public async Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataByRangeAsync(DateTime startDate, DateTime endDate, List<int>? fundingSourceIds, List<int>? vehicleRouteIds)
        {
            // 1. Initial Query with all necessary includes
            var baseQuery = _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.Trip).ThenInclude(t => t.FundingSource)
                .Include(s => s.Trip).ThenInclude(t => t.SpaceType)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Where(s => s.Date.HasValue &&
                            s.Date.Value.Date >= startDate.Date &&
                            s.Date.Value.Date <= endDate.Date &&
                            s.TripId != null);

            // 2. Apply Multi-ID Filter if the list contains IDs
            if (fundingSourceIds != null && fundingSourceIds.Any())
            {
                baseQuery = baseQuery.Where(s => fundingSourceIds.Contains(s.Trip.FundingSourceId.Value));
            }

            if (vehicleRouteIds != null && vehicleRouteIds.Any())
            {                
                baseQuery = baseQuery.Where(s => vehicleRouteIds.Contains(s.VehicleRouteId));
            }

            // Execute query to bring data into memory
            var schedulesForPeriod = await baseQuery.ToListAsync();

            // 3. Optimized Billing Rules Fetching
            // We only fetch rules for the Funding Sources and Space Types present in this specific data set
            var fsIds = schedulesForPeriod.Select(s => s.Trip.FundingSourceId).Distinct().ToList();
            var stIds = schedulesForPeriod.Select(s => s.Trip.SpaceTypeId).Distinct().ToList();

            var allBillingRules = await _context.FundingSourceBillingItems
                .Include(bi => bi.BillingItem)
                .Where(bi => fsIds.Contains(bi.FundingSourceId) && stIds.Contains(bi.SpaceTypeId))
                .ToListAsync();

            // 4. Group by TripId and project into the DTO
            var reportData = schedulesForPeriod
                .GroupBy(s => s.TripId)
                .Select(tripGroup =>
                {
                    var pickup = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Pickup);
                    var dropoff = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Dropoff);
                    var trip = pickup?.Trip ?? dropoff?.Trip;

                    if (trip == null) return null;

                    // The figure the Home tab totals for the day has to be the same figure this
                    // report prints, so both ask TripChargeCalculator rather than each working it
                    // out. Two copies of a money formula disagree quietly, and the first anyone
                    // hears of it is an invoice nobody can reconcile.
                    decimal totalBilled = TripChargeCalculator.For(
                        allBillingRules, trip.FundingSourceId, trip.SpaceTypeId, trip.Distance);

                    // C. Mapping to DTO
                    return new ProductionReportRowDto
                    {
                        Date = trip.Date,
                        ReqPickup = trip.FromTime,
                        Appointment = trip.ToTime,
                        Patient = trip.Customer?.FullName,
                        PickupAddress = trip.PickupAddress,
                        DropoffAddress = trip.DropoffAddress,
                        Space = trip.SpaceType?.Name,
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
                        WillCallTime = trip.WillCall ? trip.FromTime : null,
                        Vehicle = pickup?.VehicleRoute?.Vehicle?.Name,
                        VehiclePlate = pickup?.VehicleRoute?.Vehicle?.Plate,
                        TripId = trip.TripId,
                        PickupGpsArriveDistance = pickup?.ArriveDistance,
                        DropoffGpsArriveDistance = dropoff?.ArriveDistance,
                        PickupCity = trip.PickupCity,
                        PickupState = trip.Customer?.State,
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
                .OrderBy(row => row.Date)
                .ThenBy(row => row.Run)
                .ThenBy(row => row.ReqPickup)
                .ToList();

            return reportData;
        }

        public async Task<IEnumerable<ProductionReportRowDto>> GetProductionReportDataByRangeAsync2(DateTime startDate, DateTime endDate, List<int>? fundingSourceIds)
        {
            // 1. Initial Query with all necessary includes
            var baseQuery = _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.Trip).ThenInclude(t => t.FundingSource)
                .Include(s => s.Trip).ThenInclude(t => t.SpaceType)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Where(s => s.Date.HasValue &&
                            s.Date.Value.Date >= startDate.Date &&
                            s.Date.Value.Date <= endDate.Date &&
                            s.TripId != null);

            // 2. Apply Multi-ID Filter if the list contains IDs
            if (fundingSourceIds != null && fundingSourceIds.Any())
            {
                baseQuery = baseQuery.Where(s => fundingSourceIds.Contains(s.Trip.FundingSourceId.Value));
            }

            // Execute query to bring data into memory
            var schedulesForPeriod = await baseQuery.ToListAsync();

            // 3. Optimized Billing Rules Fetching
            // We only fetch rules for the Funding Sources and Space Types present in this specific data set
            var fsIds = schedulesForPeriod.Select(s => s.Trip.FundingSourceId).Distinct().ToList();
            var stIds = schedulesForPeriod.Select(s => s.Trip.SpaceTypeId).Distinct().ToList();

            var allBillingRules = await _context.FundingSourceBillingItems
                .Include(bi => bi.BillingItem)
                .Where(bi => fsIds.Contains(bi.FundingSourceId) && stIds.Contains(bi.SpaceTypeId))
                .ToListAsync();

            // 4. Group by TripId and project into the DTO
            var reportData = schedulesForPeriod
                .GroupBy(s => s.TripId)
                .Select(tripGroup =>
                {
                    var pickup = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Pickup);
                    var dropoff = tripGroup.FirstOrDefault(s => s.EventType == ScheduleEventType.Dropoff);
                    var trip = pickup?.Trip ?? dropoff?.Trip;

                    if (trip == null) return null;

                    // The figure the Home tab totals for the day has to be the same figure this
                    // report prints, so both ask TripChargeCalculator rather than each working it
                    // out. Two copies of a money formula disagree quietly, and the first anyone
                    // hears of it is an invoice nobody can reconcile.
                    decimal totalBilled = TripChargeCalculator.For(
                        allBillingRules, trip.FundingSourceId, trip.SpaceTypeId, trip.Distance);

                    // C. Mapping to DTO
                    return new ProductionReportRowDto
                    {
                        Date = trip.Date,
                        ReqPickup = trip.FromTime,
                        Appointment = trip.ToTime,
                        Patient = trip.Customer?.FullName,
                        PickupAddress = trip.PickupAddress,
                        DropoffAddress = trip.DropoffAddress,
                        Space = trip.SpaceType?.Name,
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
                        WillCallTime = trip.WillCall ? trip.FromTime : null,
                        Vehicle = pickup?.VehicleRoute?.Vehicle?.Name,
                        VehiclePlate = pickup?.VehicleRoute?.Vehicle?.Plate,
                        TripId = trip.TripId,
                        PickupGpsArriveDistance = pickup?.ArriveDistance,
                        DropoffGpsArriveDistance = dropoff?.ArriveDistance,
                        PickupCity = trip.PickupCity,
                        PickupState = trip.Customer?.State,
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
                .OrderBy(row => row.Date)
                .ThenBy(row => row.Run)
                .ThenBy(row => row.ReqPickup)
                .ToList();

            return reportData;
        }
        public async Task<IEnumerable<ProductionReportRowDto>> GetAviataReportDataAsync(DateTime startDate, DateTime endDate, List<int>? fundingSourceIds)
        {
            var baseQuery = _context.Schedules
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .Include(s => s.Trip).ThenInclude(t => t.FundingSource)
                .Include(s => s.Trip).ThenInclude(t => t.SpaceType)
                .Include(s => s.VehicleRoute)
                .Where(s => s.Date.HasValue && s.Date.Value.Date >= startDate.Date && s.Date.Value.Date <= endDate.Date && s.TripId != null);

            // Filter by multiple IDs if provided
            if (fundingSourceIds != null && fundingSourceIds.Any())
            {
                baseQuery = baseQuery.Where(s => fundingSourceIds.Contains(s.Trip.FundingSourceId.Value));
            }

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
                    var loadRule = rules.FirstOrDefault(r => r.BillingItem.Description.Contains("Loading") || r.BillingItem.Description.Contains("PICK UP"));
                    var milesRule = rules.FirstOrDefault(r => r.BillingItem.Description.Contains("MILES"));
                    var cancelationRule = rules.FirstOrDefault(r => r.BillingItem.Description.Contains("CANCELATION"));

                    if (trip.IsCancelled)
                    {
                        decimal percentValue = cancelationRule?.Rate ?? 25m;
                        double percent = (double)percentValue;
                        decimal baseCharge = loadRule?.Rate ?? 0m;
                        double totalCharge = (double)baseCharge;

                        double cancelationFeeRate = (totalCharge * percent) / 100.0;

                        row.BillableLines ??= new List<ChargeLineDto>();

                        row.BillableLines.Add(new ChargeLineDto
                        {
                            ChargeName = "CANCELATION FEE",
                            Quantity = 1.0,
                            Rate = cancelationFeeRate
                        });


                        /*if (cancelationRule != null)
                        {
                            percent = (double)cancelationRule.Rate.GetValueOrDefault(0m);
                        }

                        double totalCharge = (loadRule?.Rate != null) ? (double)loadRule.Rate : 0.0;
                        double cancelationFeeRate = (totalCharge * percent) / 100;
                        if (row.BillableLines == null)
                        {
                            row.BillableLines = new List<ChargeLineDto>();
                        }

                        row.BillableLines.Add(new ChargeLineDto
                        {
                            ChargeName = "CANCELATION FEE",
                            Quantity = 1.0,
                            Rate = cancelationFeeRate
                        });*/
                        //row.BillableLines.Add(new ChargeLineDto { ChargeName = "CANCELATION FEE", Quantity = 1.0, Rate = cancelationFeeRate });
                    }
                    else
                    {
                        // PICK UP FEE
                        
                        if (loadRule != null)
                        {
                            row.BillableLines.Add(new ChargeLineDto
                            {
                                ChargeName = loadRule.BillingItem.Description, // "PICK UP FEE",
                                Quantity = 1.0,
                                Rate = (double)loadRule.Rate
                            });
                        }

                        // MILES 
                        
                        if (milesRule != null)
                        {
                            double tripMiles = trip.Distance ?? 0.0;
                            int freeQty = milesRule.FreeQty ?? 0;
                            // double freeQty = (double)(milesRule.FreeQty ?? 0); // no funciona

                            // C# promocionar� el int a double autom�ticamente en la resta, manteniendo la precisi�n.
                            double billableMiles = Math.Max(0, tripMiles - freeQty);

                            // Usamos Math.Max con doubles expl�citos (No funciona)
                            //double billableMiles = Math.Max(0.0, tripMiles - freeQty); //no funciona

                            row.BillableLines.Add(new ChargeLineDto
                            {
                                ChargeName = milesRule.BillingItem.Description, // "MILES",
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

            // Obtener TODAS las reglas de cobro para los FundingSources y SpaceTypes involucrados en este d�a
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

                    // --- L�GICA DE C�LCULO DE FACTURACI�N ---
                    decimal totalBilled = 0;
                    double distance = trip.Distance ?? 0;

                    // Filtrar las reglas que aplican a ESTE viaje espec�fico
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

                    // B. MILES (C�lculo por distancia con FreeQty)
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
                    Wait = s.EarlyArrivalWait,
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
                    (s.Trip.Customer.Phone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "") == phone ||
                    s.Trip.Customer.MobilePhone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "") == phone) &&
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
        public async Task<bool> UpdateScheduleEtaAsync(int id, UpdateScheduleEtaDto dto)
        {
            var schedule = await _context.Schedules.FindAsync(id);

            if (schedule == null) return false;

            schedule.ETATime = dto.ETA;
            schedule.TravelTime = dto.Travel;

            await _context.SaveChangesAsync();
            return true;
        }

    }
}

