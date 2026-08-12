using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Raphael.Api.Settings;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.SignalR;
using Raphael.Notification.Infrastructure.Realtime.Hubs;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using NotificationModel = Raphael.Shared.Entities.Notifications.Notification;

namespace Raphael.Api.Services
{
    public class RiderService : IRiderService
    {
        private readonly RaphaelContext _context;
        private readonly JwtSettings _jwtSettings;
        private readonly IExpoPushService _expoPushService;

        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public RiderService(RaphaelContext context, IOptions<JwtSettings> jwtOptions, IHubContext<NotificationHub, INotificationClient> hubContext, IExpoPushService expoPushService    )
        {
            _context = context;
            _jwtSettings = jwtOptions.Value;
            _hubContext = hubContext;
            _expoPushService = expoPushService;
        }

        public async Task<ExpoPushResult> SendTestPushAsync(int customerId, string message)
        {
            var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null || string.IsNullOrEmpty(customer.PushToken))
            {
                return new ExpoPushResult { Success = false, ErrorMessage = "Token not found in Database." };
            }

            return await _expoPushService.SendPushNotificationWithDetailsAsync(
                //"ExponentPushToken[AA74PVN3PpPqY6KABDVth_]",
                customer.PushToken,
                "Raphael Test",
                message,
                new { tripId = 101 }
            );
        }
        /*public async Task<bool> SendTestPushAsync(int customerId, string message)
        {
            // 1. Search for the patient and their token
            var customer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == customerId);

            if (customer == null || string.IsNullOrEmpty(customer.PushToken))
            {
                return false; // There is no one to send.
            }

            // 2. Trigger the notification via Expo
            return await _expoPushService.SendPushNotificationAsync(
                customer.PushToken,
                "Raphael Update",
                message,
                new { tripId = 101, type = "test" } 
            );
        }*/

        public async Task<bool> SavePushTokenAsync(int customerId, string token)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return false;

            customer.PushToken = token;

            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                // Loguear error
                return false;
            }
        }

        /* private async Task NotifyRiderStatusChange(int customerId, string title, string message)
         {
             // Usamos el grupo que creamos en el Hub
             await _hubContext.Clients.Group($"Customer_{customerId}")
                 .ReceiveNotification(new NotificationModel
                 { // Asumiendo que tienes un NotificationModel
                     Title = title,
                     Message = message,
                     CreatedAt = DateTime.UtcNow
                 });
         }*/

        public async Task<RiderAuthResponse?> IdentifyAsync(RiderIdentifyRequest request)
        {
            // 1. Phone number cleanup (digits only)
            var cleanRequestPhone = Regex.Replace(request.Phone, @"[^\d]", "");
            var requestName = request.FullName.Trim().ToLower();

            // 2. Query with Strict Equality
            // We use Replace on the server side to compare only the numbers.
            var customer = await _context.Customers
                .Include(c => c.SpaceType)
                .Include(c => c.FundingSource)
                .FirstOrDefaultAsync(c =>
                    c.FullName.ToLower() == requestName &&
                    (
                        // Attempt 1: Direct comparison (if they are already clean in the DB)
                        c.Phone == cleanRequestPhone ||
                        c.MobilePhone == cleanRequestPhone ||

                        // Attempt 2: Dynamic SQL cleaning to ensure an exact digit match
                        c.Phone.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "") == cleanRequestPhone ||
                        c.MobilePhone.Replace("-", "").Replace("(", "").Replace(")", "").Replace(" ", "") == cleanRequestPhone
                    )
                );

            // 3. Additional security validation
            if (customer == null)
            {
                // Log failed attempt for audit purposes
                return null;
            }

            return new RiderAuthResponse
            {
                Token = GenerateRiderToken(customer),
                IsSuccess = true,
                Customer = MapToCustomerResponseDto(customer)
            };
        }

        public async Task<IEnumerable<ScheduleDto>> GetMySchedulesAsync(int customerId, DateTime date)
        {           
            return await _context.Schedules
                .AsNoTracking()
                .Include(s => s.Trip)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Vehicle)
                .Where(s => s.Date.HasValue && s.Date.Value.Date == date.Date) 
                .Where(s => s.Trip != null && s.Trip.CustomerId == customerId) 
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
                    Driver = s.VehicleRoute != null && s.VehicleRoute.Driver != null ? s.VehicleRoute.Driver.FullName : "N/A",
                    ETA = s.ETATime,
                    Distance = s.DistanceToPoint,
                    Travel = s.TravelTime,
                    Arrive = s.ActualArriveTime,
                    Perform = s.ActualPerformTime,
                    Performed = s.Performed,
                    EventType = s.EventType,
                    Run = s.VehicleRoute != null ? s.VehicleRoute.Name : "N/A",
                    Vehicle = (s.VehicleRoute != null && s.VehicleRoute.Vehicle != null) ? s.VehicleRoute.Vehicle.Name : "N/A",
                    Patient = s.Trip != null ? s.Trip.Customer.FullName : s.Name
                })
                .ToListAsync();
        }
        public async Task<IEnumerable<ScheduleDto>> GetMySchedulesAsyncError(int customerId, DateTime date)
        {
            return await _context.Schedules
                .AsNoTracking()
                .Include(s => s.Trip)
                .Include(s => s.VehicleRoute).ThenInclude(vr => vr.Driver)
                .Where(s => s.Trip.CustomerId == customerId && (!s.TripId.HasValue || s.Trip.Date.Date == date.Date))
                .OrderBy(s => s.Sequence)
                .Select(s => MapToScheduleDto(s))
                .ToListAsync();
        }

        public async Task<IEnumerable<TripReadDto>> GetMyTripHistoryAsync(int customerId, DateTime startDate, DateTime endDate)
        {
            return await _context.Trips
                .AsNoTracking()
                .Include(t => t.SpaceType)
                .Include(t => t.FundingSource)
                .Where(t => t.CustomerId == customerId && t.Date.Date >= startDate.Date && t.Date.Date <= endDate.Date)
                .OrderByDescending(t => t.Date)
                .Select(t => MapToTripReadDto(t))
                .ToListAsync();
        }

        public async Task<List<GpsDataDto>> GetMyActiveVehicleLocationAsync(int customerId)
        {
            var activeTrip = await _context.Trips
                .AsNoTracking()
                .Where(t => t.CustomerId == customerId &&
                           (t.Status == TripStatus.InProgress || t.Status == TripStatus.Waiting))
                .OrderByDescending(t => t.Created)
                .FirstOrDefaultAsync();

            if (activeTrip?.VehicleRouteId == null) return new List<GpsDataDto>();

            // RETORNA LAS ÚLTIMAS 3 POSICIONES para la animación de 30s en la app
            return await _context.GPSData
                .AsNoTracking()
                .Where(g => g.IdVehicleRoute == activeTrip.VehicleRouteId)
                .OrderByDescending(g => g.DateTime)
                .Take(3)
                .Select(g => new GpsDataDto
                {
                    Latitude = g.Latitude,
                    Longitude = g.Longitude,
                    Speed = g.Speed,
                    DateTime = g.DateTime,
                    Direction = g.Direction
                })
                .ToListAsync();
        }

        public async Task<bool> ActivateWillCallAsync(int tripId, int customerId)
        {
            var trip = await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.CustomerId == customerId);
            if (trip == null || !trip.WillCall) return false;

            trip.FromTime = DateTime.Now.TimeOfDay;
            trip.Status = TripStatus.Waiting; // Al activar Will Call, el estado pasa a Waiting para el Driver

            _context.TripLogs.Add(new TripLog
            {
                TripId = trip.Id,
                Status = TripStatus.Waiting,
                Date = DateTime.UtcNow.Date,
                Time = DateTime.UtcNow.TimeOfDay
            });

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateProfileAsync(int customerId, CustomerCreateDto dto)
        {
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null) return false;

            customer.Email = dto.Email;
            customer.Phone = dto.Phone;
            customer.MobilePhone = dto.MobilePhone;
            customer.DOB = dto.DOB;
            customer.Address = dto.Address;
            customer.City = dto.City;
            customer.Zip = dto.Zip;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SubmitRatingAsync(RatingCreateDto dto, int customerId)
        {
            var trip = await _context.Trips.AsNoTracking().FirstOrDefaultAsync(t => t.Id == dto.TripId && t.CustomerId == customerId);
            if (trip == null || trip.Status != TripStatus.Finished) return false;

            var rating = new Rating
            {
                TripId = dto.TripId,
                CustomerId = customerId,
                DriverId = dto.DriverId,
                Score = dto.Score,
                Comment = dto.Comment,
                CreatedAt = DateTime.UtcNow
            };

            _context.Ratings.Add(rating);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- PRIVATE HELPERS ---

        private string GenerateRiderToken(Customer customer)
        {
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, customer.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, customer.FullName),
                new Claim("CustomerId", customer.Id.ToString()),
                new Claim(ClaimTypes.Role, "Rider")
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(365), // Sesión persistente
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static CustomerResponseDto MapToCustomerResponseDto(Customer c) => new()
        {
            Id = c.Id,
            FullName = c.FullName,
            Address = c.Address,
            City = c.City,
            State = c.State,
            Zip = c.Zip,
            Phone = c.Phone,
            RiderId = c.RiderId,
            Email = c.Email,
            FundingSourceName = c.FundingSource?.Name
        };

        private static ScheduleDto MapToScheduleDto(Schedule s) => new()
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
         
        };

        private static TripReadDto MapToTripReadDto(Trip t) => new()
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
}