using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Realtime;
using Raphael.Api.Services;
using Raphael.Notification.Application.DTOs;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;
using Raphael.Shared.DbContexts;
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
        public async Task<IActionResult> SyncSingle([FromForm] PortalTripDto trip, [FromServices] Raphael.Api.Services.Catalog.IPortalCatalogService catalog)
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

            // A clinic may give a trip only to a Provider it contracted and that operates in Raphael.
            // Keeping the one the trip already has is always allowed: the office may have assigned it.
            // Internal broker users have no contracted list and may give it to any Provider.
            if (trip.SetProvider && trip.ProviderId is int providerId && CurrentIntegratorId != null)
            {
                var keepsCurrent = isEdit && (await _tripService.GetByIdAsync(trip.InternalId!.Value))?.ProviderId == providerId;
                if (!keepsCurrent && !await catalog.IsAssignableAsync(providerId, HttpContext.RequestAborted))
                {
                    return BadRequest("This provider cannot receive trips from this facility. Choose one from your contracted providers.");
                }
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
        [ProducesResponseType(typeof(List<TripReadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TripReadDto>>> GetMyTrips([FromQuery] DateTime startDate, [FromQuery] DateTime? endDate)
        {
            var end = endDate ?? startDate;

            // The service refuses this with an ArgumentException, which reached the clinic as a 500.
            if (end.Date < startDate.Date)
            {
                return BadRequest("The end date cannot be earlier than the start date.");
            }

            // El Global Query Filter en RaphaelContext ya se encarga de filtrar por IntegratorId
            var trips = await _tripService.GetByDateRangeAsync(startDate, end);
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

        /// <summary>One of the clinic's trips, for its tracking view: places, route ETAs and what already happened.</summary>
        /// <remarks>The Trip query filter keeps it to the caller's integrator: another clinic's trip is a 404.</remarks>
        [HttpGet("trips/{id:int}/tracking")]
        [ProducesResponseType(typeof(TripTrackingDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TripTrackingDto>> GetTripTracking(int id, [FromServices] RaphaelContext context)
        {
            var trip = await context.Trips
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new TripTrackingDto
                {
                    TripId = t.Id,
                    Status = t.Status,
                    IsCancelled = t.IsCancelled,
                    Date = t.Date,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    RequestedPickupTime = t.FromTime,
                    AppointmentTime = t.ToTime,
                    // The provider and the kind of vehicle, never the route's or the vehicle's own
                    // names: those carry the driver's name and fleet codes (TripTrackingDto.VehicleType).
                    ProviderName = t.Provider != null ? t.Provider.Name : null,
                    VehicleType = t.Run != null ? t.Run.Vehicle.VehicleType.Name : null
                })
                .FirstOrDefaultAsync();

            if (trip == null) return NotFound();

            trip.InProgress = !trip.IsCancelled && TripTracking.IsUnderWay(trip.Status);

            var stops = await context.Schedules
                .AsNoTracking()
                .Where(s => s.TripId == id)
                .Select(s => new { s.EventType, s.ETATime, s.ActualArriveTime, s.ActualPerformTime })
                .ToListAsync();

            var pickup = stops.FirstOrDefault(s => s.EventType == ScheduleEventType.Pickup);
            var dropoff = stops.FirstOrDefault(s => s.EventType == ScheduleEventType.Dropoff);

            trip.PickupEta = pickup?.ETATime;
            trip.PickupArrivedAt = pickup?.ActualArriveTime;
            trip.PickedUpAt = pickup?.ActualPerformTime;
            trip.DropoffEta = dropoff?.ETATime;
            trip.DropoffArrivedAt = dropoff?.ActualArriveTime;
            trip.DroppedOffAt = dropoff?.ActualPerformTime;

            return Ok(trip);
        }

        [HttpGet("my-funding-source")]
        [ProducesResponseType(typeof(FundingSource), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<FundingSource>> GetMyFundingSource([FromServices] IIntegratorService integratorService)
        {
            var fundingSource = await integratorService.GetFundingSourceByIntegratorIdAsync(CurrentIntegratorId);
            if (fundingSource == null) return NotFound("No funding source linked to this integrator.");

            return Ok(fundingSource);
        }

        /// <summary>
        /// The clinic's notices: what its integrator is told, the same rows the API Key integration reads.
        /// </summary>
        /// <remarks>
        /// Expired ones are not returned, so the window is the retention policy's for an integration
        /// (seven days). Read state is not kept here: the recipient is the whole integrator, shared by
        /// every user of the clinic and by its API Key client, so the portal keeps it per user.
        /// </remarks>
        [HttpGet("notifications")]
        [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetMyNotifications(
            [FromServices] GetRecipientNotificationsHandler handler,
            CancellationToken cancellationToken)
        {
            // An office session has no integrator: it has its own inbox in the Desktop.
            if (CurrentIntegratorId is not > 0) return Forbid();

            var result = await handler.Handle(
                new GetRecipientNotificationsQuery(
                    UserIdentifierConverter.ToGuid(CurrentIntegratorId.Value, RecipientType.Integration),
                    RecipientType.Integration),
                cancellationToken);

            return Ok(result);
        }
    }
}