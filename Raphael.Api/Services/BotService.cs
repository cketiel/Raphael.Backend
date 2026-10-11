using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Notifications;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;
using System.Text.RegularExpressions;

namespace Raphael.Api.Services
{
    public class BotService : IBotService
    {
        private readonly RaphaelContext _context;
        private readonly ITripNotificationPublisher _tripNotifications;
        private readonly IOperationClock _clock;

        public BotService(
            RaphaelContext context,
            ITripNotificationPublisher tripNotifications,
            IOperationClock clock)
        {
            _context = context;
            _tripNotifications = tripNotifications;
            _clock = clock;
        }
        public async Task<string> ActivateWillCallAsync(string tripNumber)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == tripNumber);
            if (trip == null)
            {
                return "TRIP_NOT_FOUND"; // Trip not found
            }
            else if (!trip.WillCall) {
                return "ALREADY_ACTIVE";
            }

            // The one hour commitment starts here, not when a dispatcher reads the notice.
            var activatedAtUtc = DateTime.UtcNow;

            try
            {
                string priorValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

                // Wall-clock time where the trip is operated, not where this server is
                // hosted. A dispatcher reading a pickup fixed three hours in the past has
                // an hour to get a vehicle there and no idea it already started.
                trip.FromTime = _clock.TimeOfDayFor(trip.ProviderId);
                trip.Status = TripStatus.Waiting; // When Will Call is activated, the status changes to "Waiting" for the driver.
                trip.WillCall = false; // Mark WillCall as false since it's now activated

                string newValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

                _context.TripLogs.Add(new TripLog
                {
                    TripId = trip.Id,
                    Status = TripStatus.Waiting,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay
                });
                // Make an entry in the history log to track who activated a Will Call and when.
                _context.TripHistories.Add(new TripHistory
                {
                    TripId = trip.Id,
                    User = "Bot - RaphaelCustomerServiceBot",
                    Field = "WillCall",
                    PriorValue = priorValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return "CANNOT_ACTIVATE";
            }

            // Outside the catch: this method reports failure through its return value,
            // and a notification problem must not make the bot tell a patient their
            // Will Call was not registered when it was.
            //
            // Somebody rang on the patient's behalf, so the patient is told too.
            await _tripNotifications.WillCallActivatedAsync(
                trip,
                activatedAtUtc,
                notifyRider: true);

            return "SUCCESS";
        }
        public async Task<string> CancelTripAsync(string tripNumber)
        {           
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.TripId == tripNumber);
            if (trip == null)
            {
                return "TRIP_NOT_FOUND"; // Trip not found
            }

            // You cannot cancel a trip that has already been completed or cancelled or Billed or Payed.
            if (trip.Status == TripStatus.Finished || trip.Status == TripStatus.Canceled || trip.Status == TripStatus.Billed || trip.Status == TripStatus.Payed)
            {
                return "CANNOT_CANCEL";
            }

            var statusBeforeCancellation = trip.Status;

            try
            {
                string priorValue = $"trip.Status={trip.Status}, trip.IsCancelled={trip.IsCancelled}";

                trip.Status = TripStatus.Canceled;
                trip.IsCancelled = true;

                string newValue = $"trip.Status={trip.Status}, trip.IsCancelled={trip.IsCancelled}";

                // Create the status change log.
                var tripLog = new TripLog
                {
                    TripId = trip.Id,
                    Status = TripStatus.Canceled,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay
                };
                _context.TripLogs.Add(tripLog);
                _context.TripHistories.Add(new TripHistory
                {
                    TripId = trip.Id,
                    User = "Bot - RaphaelCustomerServiceBot",
                    Field = "Status",
                    PriorValue = priorValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                return "CANNOT_CANCEL";
            }

            await _tripNotifications.TripCancelledAsync(
                trip,
                CancelledByTypes.Bot,
                statusBeforeCancellation);

            return "SUCCESS";
        }

        public async Task<TimeSpan?> GetEtaAsync(string tripNumber)
        {
            return await _context.Schedules
                .Where(s => s.Trip.TripId == tripNumber.Trim() && !s.Performed && s.EventType == ScheduleEventType.Pickup && s.Trip.Status != TripStatus.Canceled)
                .Select(s => s.ETATime).FirstOrDefaultAsync();
                        
        }
        public async Task<TimeSpan?> GetEtaAsync(string? patientFullName, string? phone, DateTime? date, string? tripNumber)
        {
            var query = _context.Schedules
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Include(s => s.Trip).ThenInclude(t => t.Customer)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(tripNumber))
            {

                query = query.Where(s => s.Trip.TripId == tripNumber.Trim() && !s.Performed && s.EventType == ScheduleEventType.Pickup && s.Trip.Status != TripStatus.Canceled);
            }
            else
            {
                // Search by Profile (Name + Phone + Date)
                string pName = patientFullName.ToLower().Trim();
                query = query.Where(s =>
                    s.Trip.Customer.FullName.ToLower().Contains(pName) &&
                    (s.Trip.Customer.Phone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "") == phone ||
                    s.Trip.Customer.MobilePhone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "") == phone) &&
                    s.Date.Value.Date == date.Value.Date && !s.Performed && s.EventType == ScheduleEventType.Pickup && s.Trip.Status != TripStatus.Canceled
                );
            }

            return await query.Select(s => s.ETATime).FirstOrDefaultAsync();

        }

        public async Task<string> GetEtaUnavailableReasonAsync(string? patientFullName, string? phone, DateTime? date, string? tripNumber)
        {
            // Runs only after the ETA lookup above came back empty, and never changes what that
            // lookup returns: the bot in production keeps reading success/message/eta as before,
            // and the reason is an extra field it can start reading when it is ready.
            var trips = _context.Trips.AsQueryable();

            if (!string.IsNullOrWhiteSpace(tripNumber))
            {
                string number = tripNumber.Trim();
                trips = trips.Where(t => t.TripId == number);
            }
            else
            {
                // Same match as the profile lookup, but on the trip's own date: a trip that is not
                // on a route yet has no schedule to carry one.
                string pName = patientFullName!.ToLower().Trim();
                trips = trips.Where(t =>
                    t.Customer.FullName.ToLower().Contains(pName) &&
                    (t.Customer.Phone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "") == phone ||
                     t.Customer.MobilePhone.Replace("-", "").Replace(" ", "").Replace("(", "").Replace(")", "") == phone) &&
                    t.Date.Date == date!.Value.Date);
            }

            var candidates = await trips
                .Select(t => new
                {
                    t.FromTime,
                    t.WillCall,
                    Canceled = t.IsCancelled || t.Status == TripStatus.Canceled,
                    PickedUp = t.Status == TripStatus.InProgress || t.Status == TripStatus.Finished ||
                               t.Status == TripStatus.Billed || t.Status == TripStatus.Payed ||
                               _context.Schedules.Any(s => s.TripId == t.Id && s.EventType == ScheduleEventType.Pickup && s.Performed),
                    HasPickup = _context.Schedules.Any(s => s.TripId == t.Id && s.EventType == ScheduleEventType.Pickup)
                })
                .ToListAsync();

            if (candidates.Count == 0) return BotEtaReasons.TripNotFound;

            var live = candidates.Where(c => !c.Canceled).ToList();
            if (live.Count == 0) return BotEtaReasons.TripCanceled;

            // A patient with several trips that day is asking about the one still to come, so a
            // trip already picked up only explains the missing ETA when every trip is.
            var pending = live.Where(c => !c.PickedUp).OrderBy(c => c.FromTime).FirstOrDefault();
            if (pending == null) return BotEtaReasons.PickupCompleted;

            if (pending.WillCall) return BotEtaReasons.TripIsWillCall;
            if (!pending.HasPickup) return BotEtaReasons.TripNotScheduled;

            // On a route, not picked up, and still no ETA: the route's times have not been
            // calculated yet, or something nobody has named. Either way the office has to look.
            return BotEtaReasons.Unknown;
        }

    }

    /// <summary>
    /// Why the bot got no ETA. Returned as <c>reason</c> next to <c>ETA_NOT_AVAILABLE</c>.
    /// </summary>
    /// <remarks>
    /// A contract with the Customer Service Bot (see _meta/BOT_API_SPEC.md): a value is never
    /// renamed or removed, and a new one is announced to the bot team before it ships.
    /// </remarks>
    public static class BotEtaReasons
    {
        public const string TripNotFound = "TRIP_NOT_FOUND";
        public const string TripCanceled = "TRIP_CANCELED";
        public const string PickupCompleted = "PICKUP_COMPLETED";
        public const string TripIsWillCall = "TRIP_IS_WILL_CALL";
        public const string TripNotScheduled = "TRIP_NOT_SCHEDULED";
        public const string Unknown = "UNKNOWN";
    }
}
