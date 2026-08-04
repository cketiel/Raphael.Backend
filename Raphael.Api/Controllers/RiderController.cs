using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Raphael.Api.Services;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Infrastructure.Realtime.Contracts;
using Raphael.Notification.Infrastructure.Realtime.Hubs;
using Raphael.Notification.Infrastructure.Realtime.Models;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RiderController : ControllerBase
    {
        private readonly IRiderService _riderService;

        private readonly IHubContext<NotificationHub, INotificationClient> _hubContext;

        public RiderController(IRiderService riderService, IHubContext<NotificationHub, INotificationClient> hubContext)
        {
            _riderService = riderService;
            _hubContext = hubContext;
        }

        [AllowAnonymous] // <--- Ahora es público para facilitar tus pruebas
        [HttpPost("test-notification-anonymous/{targetCustomerId}")]
        public async Task<IActionResult> SendTestNotificationAnonymous(int targetCustomerId, [FromQuery] string message)
        {
            // 1. Construir el DTO (Usamos tus modelos de Raphael.Notification)
            var notificationDto = new NotificationDto
            {
                Id = Guid.NewGuid(),
                BusinessEventCode = "RIDER_EXTERNAL_TEST",
                Title = "Raphael Dispatch",
                Message = message ?? "El conductor está en camino a su ubicación.",
                Priority = NotificationPriority.High.Name,
                Severity = NotificationSeverity.Warning.Name, // Esto debería activar un Warning en la App
                Type = NotificationType.Alert.Name,
                Status = NotificationStatus.Delivered.Name,
                CreatedAtUtc = DateTime.UtcNow
            };

            // 2. Disparar al grupo basado en el parámetro 'targetCustomerId'
            // IMPORTANTE: El nombre del grupo debe ser idéntico al que genera el Hub: "Customer_123"
            string groupName = $"Customer_{targetCustomerId}";

            await _hubContext.Clients.Group(groupName)
                .ReceiveNotification(notificationDto);

            return Ok(new
            {
                Info = "Notificación enviada sin necesidad de token",
                TargetGroup = groupName,
                Payload = notificationDto
            });
        }

        [Authorize(Roles = "Rider")]
        [HttpPost("test-notification")]
        public async Task<IActionResult> SendTestNotification([FromQuery] string message)
        {
         
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim))
                return BadRequest("No CustomerId found in token.");

            
            var notificationDto = new NotificationDto
            {
                Id = Guid.NewGuid(),
                BusinessEventCode = "RIDER_TEST_EVENT",
                Title = "Raphael System",
                Message = message ?? "Test message from Raphael Ecosystem",
                Priority = NotificationPriority.High.Name,      // "High"
                Severity = NotificationSeverity.Information.Name, // "Information"
                Type = NotificationType.Alert.Name,             // "Alert"
                Status = NotificationStatus.Delivered.Name,      // "Delivered"
                CreatedAtUtc = DateTime.UtcNow
            };
           
            await _hubContext.Clients.Group($"Customer_{customerIdClaim}")
                .ReceiveNotification(notificationDto);

            return Ok(new { Group = $"Customer_{customerIdClaim}", Sent = notificationDto });
        }

        [AllowAnonymous]
        [HttpPost("auth/identify")]
        public async Task<IActionResult> Identify([FromBody] RiderIdentifyRequest request)
        {
            var response = await _riderService.IdentifyAsync(request);
            return response == null ? Unauthorized("Patient not found in Raphael Ecosystem.") : Ok(response);
        }

        [Authorize(Roles = "Rider")]
        [HttpGet("schedules")]
        public async Task<IActionResult> GetSchedules([FromQuery] DateTime date)
        {
            var customerId = GetCurrentCustomerId();
            return Ok(await _riderService.GetMySchedulesAsync(customerId, date));
        }

        [Authorize(Roles = "Rider")]
        [HttpGet("history")]
        public async Task<IActionResult> GetHistory([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var customerId = GetCurrentCustomerId();
            return Ok(await _riderService.GetMyTripHistoryAsync(customerId, start, end));
        }

        [Authorize(Roles = "Rider")]
        [HttpGet("active-location")]
        public async Task<IActionResult> GetLocation()
        {
            var customerId = GetCurrentCustomerId();
            var locations = await _riderService.GetMyActiveVehicleLocationAsync(customerId);
            return locations.Any() ? Ok(locations) : NotFound("No active transport to track.");
        }

        [Authorize(Roles = "Rider")]
        [HttpPost("trips/{tripId}/activate-will-call")]
        public async Task<IActionResult> WillCall(int tripId)
        {
            var customerId = GetCurrentCustomerId();
            var success = await _riderService.ActivateWillCallAsync(tripId, customerId);
            return success ? Ok() : BadRequest("Could not activate Will Call.");
        }

        [Authorize(Roles = "Rider")]
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] CustomerCreateDto dto)
        {
            var customerId = GetCurrentCustomerId();
            var success = await _riderService.UpdateProfileAsync(customerId, dto);
            return success ? NoContent() : NotFound();
        }

        [Authorize(Roles = "Rider")]
        [HttpPost("ratings")]
        public async Task<IActionResult> PostRating([FromBody] RatingCreateDto dto)
        {
            var customerId = GetCurrentCustomerId();
            var success = await _riderService.SubmitRatingAsync(dto, customerId);
            return success ? Ok() : BadRequest("Invalid rating submission.");
        }

        private int GetCurrentCustomerId() => int.Parse(User.FindFirst("CustomerId")?.Value ?? "0");
    }
}