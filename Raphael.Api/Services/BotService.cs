using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using System.Text.RegularExpressions;

namespace Raphael.Api.Services
{
    public class BotService : IBotService
    {
        private readonly RaphaelContext _context;      

        public BotService(RaphaelContext context)
        {
            _context = context;          
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

            try 
            {
                string priorValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

                trip.FromTime = DateTime.Now.TimeOfDay;
                trip.Status = TripStatus.Waiting;
                trip.WillCall = false; // Mark WillCall as false since it's now activated

                string newValue = $"trip.WillCall={trip.WillCall}, trip.FromTime={trip.FromTime}, trip.Status={trip.Status}";

                _context.TripLogs.Add(new TripLog
                {
                    TripId = trip.Id,
                    Status = TripStatus.Waiting,
                    Date = DateTime.UtcNow.Date,
                    Time = DateTime.UtcNow.TimeOfDay
                });
                _context.TripHistories.Add(new TripHistory
                {
                    TripId = trip.Id,
                    User = "RaphaelCustomerServiceBot",
                    Field = "WillCall",
                    PriorValue = priorValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return "SUCCESS";
            }
            catch (Exception ex)
            {               
                return "CANNOT_ACTIVATE";
            }
            
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
                    User = "RaphaelCustomerServiceBot",
                    Field = "Status",
                    PriorValue = priorValue,
                    NewValue = newValue,
                    ChangeDate = DateTime.Now
                });

                await _context.SaveChangesAsync();

                return "SUCCESS";
            }
            catch (Exception ex)
            {
                return "CANNOT_CANCEL";
            }
            
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
      
    }
}
