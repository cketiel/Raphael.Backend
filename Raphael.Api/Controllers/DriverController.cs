using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services;
using Raphael.Shared.DbContexts;
using System.Security.Claims;

namespace Raphael.Api.Controllers
{
    //[Authorize] // Requiere JWT del Driver
    [ApiController]
    [Route("api/[controller]")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverService _driverService;
        private readonly RaphaelContext _context;
        private readonly IFirebaseMessagingService _firebaseMessagingService;

        public DriverController(IDriverService driverService, RaphaelContext context, IFirebaseMessagingService firebaseMessagingService)
        {
            _driverService = driverService;
            _context = context;
            _firebaseMessagingService = firebaseMessagingService;
        }

        [Authorize]
        [HttpPost("test-driver-push")]
        public async Task<IActionResult> TestDriverPush([FromQuery] string message)
        {          
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("UserId")?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            if (!int.TryParse(userIdStr, out var currentUserId)) return Unauthorized();

            var user = await _context.Users.FindAsync(currentUserId);

            if (user == null || string.IsNullOrEmpty(user.PushToken))
                return BadRequest("The driver does not have a registered PushToken.");

            var success = await _firebaseMessagingService.SendNotificationToDriverAsync(
                user.PushToken,
                "Test Raphael.Driver",
                message ?? "This is a test notification for the MAUI app.",
                new Dictionary<string, string> { { "test", "true" } }
            );

            return success ? Ok("Notification sent to Firebase") : Problem("Sending to Firebase failed.");
        }

        [HttpPost("push-token")]
        public async Task<IActionResult> UpdateToken([FromBody] string token)
        {          
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("UserId")?.Value;

            // User.Id is an int. Parsing it as a Guid always failed, so no driver ever
            // managed to register a device and push towards Raphael.Driver never worked.
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                return Unauthorized();

            var success = await _driverService.UpdatePushTokenAsync(userId, token);
            return success ? Ok() : NotFound("User not found.");
        }
    }
}

/*
 public async Task<bool> AssignRunAsync(int id, int? vehicleRouteId)
{
    // 1. Buscar el viaje con los datos necesarios
    var trip = await _context.Trips.FindAsync(id);
    if (trip == null) return false;

    trip.VehicleRouteId = vehicleRouteId;
    trip.Status = vehicleRouteId.HasValue ? TripStatus.Scheduled : TripStatus.Accepted;

    await _context.SaveChangesAsync();

    // 2. LÓGICA DE NOTIFICACIÓN PUSH AL CONDUCTOR
    if (vehicleRouteId.HasValue)
    {
        // Ejecutamos en un bloque try-catch para que un fallo en la red 
        // de Firebase no rompa la transacción de la base de datos
        _ = Task.Run(async () => 
        {
            try 
            {
                // Buscamos la ruta y el conductor asociado
                var route = await _context.VehicleRoutes
                    .Include(vr => vr.Driver)
                    .FirstOrDefaultAsync(vr => vr.Id == vehicleRouteId.Value);

                if (route?.Driver != null && !string.IsNullOrEmpty(route.Driver.PushToken))
                {
                    var data = new Dictionary<string, string>
                    {
                        { "tripId", trip.Id.ToString() },
                        { "action", "REFRESH_TRIPS" } // Instrucción para la App MAUI
                    };

                    await _firebaseMessagingService.SendNotificationToDriverAsync(
                        route.Driver.PushToken,
                        "Raphael Driver - Nuevo Viaje",
                        $"Se le ha asignado un nuevo viaje en: {trip.PickupAddress}",
                        data
                    );
                }
            }
            catch (Exception ex)
            {
                // Aquí loguearías el error de notificación
            }
        });
    }

    return true;
}
 */