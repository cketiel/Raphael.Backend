using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers
{
    [Authorize(Roles = "6,1,3")] // Booking
    [ApiController]
    [Route("api/[controller]")]
    public class BookingPortalController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITripHistoryService _historyService;

        public BookingPortalController(
            ITripService tripService,
            ICurrentUserService currentUserService,
            ITripHistoryService historyService) 
        {
            _tripService = tripService;
            _currentUserService = currentUserService;
            _historyService = historyService;
        }

        private int? CurrentIntegratorId => _currentUserService.IntegratorId;

        [HttpPost("sync-single")]
        public async Task<IActionResult> SyncSingle([FromForm] PortalTripDto trip)
        {
            if (trip == null) return BadRequest("Trip data is required.");

            // If it is neither an Integrator nor an Internal Administrator, then it is not authorized.
            if (CurrentIntegratorId == null && !_currentUserService.IsMilanesInternal)
            {
                return Unauthorized("You do not have permission to perform this operation.");
            }

            try
            {
                bool isEdit = trip.InternalId.HasValue && trip.InternalId > 0;
                string oldStatus = "N/A";

                // --- RESTRICCIÓN DE SEGURIDAD PARA EDICIÓN ---
                if (isEdit)
                {                  
                    var existingTrip = await _tripService.GetByIdAsync(trip.InternalId.Value);

                    if (existingTrip == null) return NotFound("Trip not found.");

                    // Solo permitir editar si el estado es Accepted o Assigned                   
                    var status = existingTrip.Status?.Trim();
                    if (status != "Accepted" && status != "Assigned")
                    {
                        return BadRequest($"Restricted: Trips in '{status}' status cannot be edited via portal. Only Accepted or Assigned.");
                    }
                    oldStatus = status;
                }
               
                var results = await _tripService.UpsertPortalTripsAsync(new List<PortalTripDto> { trip }, CurrentIntegratorId);

                string user = _currentUserService.UserName ?? "PortalUser";
                user = $"BookingWeb - {user}";

                // --- REGISTRO EN HISTORIAL (TripHistory) ---
                if (results != null && results.Any())
                {
                    foreach (var idStr in results)
                    {
                        if (int.TryParse(idStr, out int tripIdInt))
                        {
                            await _historyService.PostHistory(new TripHistory
                            {
                                TripId = tripIdInt,
                                User = user,
                                Field = "PortalSync",
                                PriorValue = isEdit ? $"Status: {oldStatus}" : "New Trip",
                                NewValue = isEdit ? "Trip Updated" : "Trip Created",
                                ChangeDate = DateTime.Now
                            });
                        }
                    }
                }

                return Ok(new { Success = true, TripIds = results });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("my-trips")]
        public async Task<IActionResult> GetMyTrips([FromQuery] DateTime startDate, [FromQuery] DateTime? endDate)
        {
            // El Global Query Filter en RaphaelContext ya se encarga de filtrar por IntegratorId
            var trips = await _tripService.GetByDateRangeAsync(startDate, endDate ?? startDate);
            return Ok(trips);
        }

        [HttpPost("cancel-multiple")]
        public async Task<IActionResult> CancelMultiple([FromBody] List<string> externalIds)
        {
            if (externalIds == null || !externalIds.Any()) return BadRequest("No IDs provided.");

            try
            {
                // --- RESTRICCIÓN DE SEGURIDAD PARA CANCELACIÓN ---
                // Obtenemos los detalles de los viajes para validar sus estados actuales
                var tripDetails = await _tripService.GetIntegrationTripDetailsAsync(null, externalIds, CurrentIntegratorId);

                // Estados permitidos para cancelar
                var allowedStatuses = new List<string> { "Accepted", "Assigned", "Scheduled" };

                // Filtramos solo los IDs que cumplen la condición de estado
                var validIdsToCancel = tripDetails
                    .Where(t => allowedStatuses.Contains(t.Status ?? ""))
                    .Select(t => t.TripId)
                    .ToList();

                if (!validIdsToCancel.Any())
                {
                    return BadRequest("The selected trips cannot be canceled because their current status does not allow it.");
                }

                string user = _currentUserService.UserName ?? "PortalUser";
                user = $"BookingWeb - {user}";

                // Ejecutamos la cancelación solo para los permitidos
                // The Booking Portal is used by the clinics, not by the integrating system.
                var count = await _tripService.CancelIntegrationTripsAsync(validIdsToCancel, CurrentIntegratorId, user, CancelledByTypes.Facility);

                

                // --- REGISTRO EN HISTORIAL ---
                foreach (var trip in tripDetails.Where(t => validIdsToCancel.Contains(t.TripId)))
                {
                    await _historyService.PostHistory(new TripHistory
                    {
                        TripId = trip.Id,
                        User = user,
                        Field = "Status",
                        PriorValue = trip.Status,
                        NewValue = "Canceled",
                        ChangeDate = DateTime.Now
                    });
                }

                return Ok(new { Success = true, CancelledCount = count, Attempted = externalIds.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }

        [HttpGet("my-funding-source")]
        public async Task<IActionResult> GetMyFundingSource([FromServices] IIntegratorService integratorService)
        {
            var fundingSource = await integratorService.GetFundingSourceByIntegratorIdAsync(CurrentIntegratorId);
            if (fundingSource == null) return NotFound("No funding source linked to this integrator.");

            return Ok(fundingSource);
        }
    }
}