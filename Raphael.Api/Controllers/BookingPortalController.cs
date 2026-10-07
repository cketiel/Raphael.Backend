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

            // No catch: the global handler logs the exception and answers with a bare
            // ProblemDetails. An exception's text can carry patient data, and it must never
            // reach a clinic's browser.
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

            // The return leg is created, never updated: nothing links it to its outbound trip,
            // so resubmitting an existing trip as a round trip would book a second return —
            // a second vehicle and a second invoice. Once it exists, it is edited on its own.
            // Without a time the upsert skips the return without a word, and the patient is
            // left at the clinic with nobody booked to bring them home.
            if (trip.IsRoundTrip && !trip.ReturnTime.HasValue)
            {
                return BadRequest("A round trip needs a return time.");
            }

            if (trip.IsRoundTrip && await OutboundTripExistsAsync(trip, isEdit))
            {
                return BadRequest("This trip already exists, so its return cannot be created again. Edit the return trip on its own.");
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
                            ChangeDate = DateTime.UtcNow
                        });
                    }
                }
            }

            return Ok(new { Success = true, TripIds = results });
        }

        /// <summary>
        /// Whether the upsert would update an existing trip rather than create one — the same two
        /// lookups <c>ProcessSingleTripAsync</c> makes: the internal id, then the clinic's own id.
        /// </summary>
        private async Task<bool> OutboundTripExistsAsync(PortalTripDto trip, bool isEdit)
        {
            if (isEdit) return true;

            if (string.IsNullOrWhiteSpace(trip.TripId)) return false;

            var existing = await _tripService.GetIntegrationTripDetailsAsync(
                null, new List<string> { trip.TripId }, CurrentIntegratorId);

            return existing.Any();
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

            // No catch: the global handler logs the exception and answers with a bare
            // ProblemDetails. An exception's text can carry patient data, and it must never
            // reach a clinic's browser.
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
                    ChangeDate = DateTime.UtcNow
                });
            }

            return Ok(new { Success = true, CancelledCount = count, Attempted = externalIds.Count });
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