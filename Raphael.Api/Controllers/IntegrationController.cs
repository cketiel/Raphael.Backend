using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Attributes;
using Raphael.Api.Services;
using Raphael.Api.Services.Notifications;
using Raphael.Notification.Application.Commands.MarkNotificationAcknowledged;
using Raphael.Notification.Application.Commands.MarkNotificationViewed;
using Raphael.Notification.Application.Helpers;
using Raphael.Notification.Application.Queries.GetRecipientNotifications;
using Raphael.Shared.Definitions.Notifications;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using System.Collections.Generic;
using System.Net.Mime;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// Specialized controller for third-party integrations (e.g., Ryde Central).
    /// Provides endpoints for trip synchronization, status monitoring, and batch cancellation.
    /// All operations are isolated by Integrator ID via API Key authentication.
    /// </summary>
    
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Bypasses Global JWT; security is handled by IntegrationApiKey attribute
    [IntegrationApiKey] // Dynamic API Key security based on database lookup
    [Produces(MediaTypeNames.Application.Json)]
    public class IntegrationController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ITripHistoryService _historyService;
        private readonly IIntegrationHubTokenService _hubTokenService;
        private readonly GetRecipientNotificationsHandler _getRecipientNotificationsHandler;
        private readonly MarkNotificationViewedHandler _markViewedHandler;
        private readonly MarkNotificationAcknowledgedHandler _markAcknowledgedHandler;

        /// <summary>
        /// Retrieves the Integrator ID stored in the HttpContext by the security filter.
        /// </summary>
        private int? CurrentIntegratorId =>
            HttpContext.Items.TryGetValue("IntegratorId", out var id) && id is int value ? value : null;

        private string? CurrentIntegratorName =>
            HttpContext.Items.TryGetValue("IntegratorName", out var name) ? name as string : null;
        public IntegrationController(
            ITripService tripService,
            ICurrentUserService currentUserService,
            ITripHistoryService tripHistoryService,
            IIntegrationHubTokenService hubTokenService,
            GetRecipientNotificationsHandler getRecipientNotificationsHandler,
            MarkNotificationViewedHandler markViewedHandler,
            MarkNotificationAcknowledgedHandler markAcknowledgedHandler)
        {
            _tripService = tripService;
            _currentUserService = currentUserService;
            _historyService = tripHistoryService;
            _hubTokenService = hubTokenService;
            _getRecipientNotificationsHandler = getRecipientNotificationsHandler;
            _markViewedHandler = markViewedHandler;
            _markAcknowledgedHandler = markAcknowledgedHandler;
        }

        /// <summary>
        /// Synchronizes multiple trips in a single batch request using Form-Data.
        /// </summary>
        /// <remarks>
        /// This endpoint performs an <b>Upsert</b> (Update or Insert) operation:
        /// <ul>
        /// <li>If the <b>TripId</b> exists for your integrator, the trip is updated.</li>
        /// <li>If the <b>TripId</b> does not exist, a new trip is created and linked to your account.</li>
        /// <li>Customers are identified by RiderId (or FullName + Phone if RiderId is missing).</li>
        /// <li>SpaceTypes and FundingSources are identified by their Name.</li>
        /// <li><b>Attachments:</b> You can upload Word or PDF files for each trip.</li>
        /// </ul>
        /// <b>Attachment Handling:</b>
        /// The 'Attachment' field expects a binary file (PDF or Word). When you upload a file:
        /// <ul>
        /// <li><b>FileContent:</b> This is the raw binary data of your file. Do not send as Base64; send as a binary part of the multipart form.</li>
        /// <li><b>FileName:</b> Automatically extracted from the uploaded file's metadata.</li>
        /// <li><b>ContentType:</b> Automatically detected (e.g., application/pdf).</li>
        /// </ul>
        /// <b>Batch Format:</b> For multiple trips, use indexed keys: trips[0].TripId, trips[0].Attachment, trips[1].TripId, etc.
        /// <b>Requirement:</b> This endpoint requires <i>multipart/form-data</i> because it handles binary file transfers.
        /// </remarks>
        /// <param name="trips">List of trip objects. Use indexed naming (e.g., trips[0].TripId, trips[0].Attachment).</param>
        /// <b>Partial success:</b> trips are stored one by one and reported one by one.
        /// A rejected trip stores nothing and does not stop the rest of the batch, so read
        /// <i>Results</i> rather than the status code alone. Each rejected trip carries an
        /// <i>ErrorCode</i> to branch on, a message describing what to fix, a <i>Retryable</i>
        /// flag, and a <i>CorrelationId</i> to quote when contacting support.
        /// <response code="200">Every trip in the batch was stored.</response>
        /// <response code="207">Some trips were stored and some were rejected. See Results.</response>
        /// <response code="400">The request is empty or contains malformed data.</response>
        /// <response code="401">Unauthorized. API Key is invalid or missing.</response>
        /// <response code="422">Every trip in the batch was rejected. See Results.</response>
        [HttpPost("sync")]
        [ProducesResponseType(typeof(IntegrationSyncResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(IntegrationSyncResultDto), StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(IntegrationSyncResultDto), StatusCodes.Status422UnprocessableEntity)]
        //To handle files (PDF/Word), the request format was changed from JSON to Multipart Form-Data. This was necessary because the JSON protocol is not efficient for sending binary files.
        public async Task<IActionResult> Sync([FromForm] List<IntegrationTripDto> trips) // Critical Change: Changed [FromBody] to [FromForm]. When files are sent, the data does not travel as a flat JSON in the body, but as a form with parts (multipart).
        {
            if (trips == null || !trips.Any())
            {
                return BadRequest("The trip list cannot be empty.");
            }

            // No catch here on purpose. Failures that belong to one trip are translated and
            // reported inside the batch; anything that escapes is a fault of ours, and the
            // global handler logs it in full and answers with ProblemDetails. Swallowing it
            // here to echo ex.Message is what hid the real cause and told the integrator
            // only that "an error occurred while saving the entity changes".
            var result = await _tripService.UpsertIntegrationTripsAsync(trips, CurrentIntegratorId, CurrentIntegratorName);

            if (result.FailedCount == 0)
            {
                return Ok(result);
            }

            return result.ProcessedCount == 0
                ? UnprocessableEntity(result)
                : StatusCode(StatusCodes.Status207MultiStatus, result);
        }

        /// <summary>
        /// Synchronizes a single trip record.
        /// </summary>
        /// <remarks>
        /// Use this endpoint for real-time single trip updates or when uploading individual attachments.
        /// Requires <i>multipart/form-data</i>.
        /// </remarks>
        /// <param name="trip">The trip object containing metadata and optional attachment.</param>
        /// <response code="200">Trip successfully synchronized.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPost("sync-single")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SyncSingle([FromForm] IntegrationTripDto trip)
        {
            if (trip == null) return BadRequest("Trip data is required.");

            return await Sync(new List<IntegrationTripDto> { trip });
        }

        /// <summary>
        /// Cancels multiple trips using your system's external identifiers (TripId).
        /// </summary>
        /// <remarks>
        /// This endpoint allows you to perform a batch cancellation. 
        /// Updates the status of the specified trips to <b>Canceled</b>.
        /// <b>Privacy Notice:</b> Only trips belonging to your Integrator profile will be modified.
        /// </remarks>
        /// <param name="externalIds">A list of strings containing the external TripIds to be canceled.</param>
        /// <returns>A JSON object indicating the total count of trips that were successfully canceled.</returns>
        /// <response code="200">Returns the count of trips updated to 'Canceled' status.</response>
        /// <response code="401">Unauthorized. The API Key is invalid or has been revoked.</response>
        /// <response code="500">Internal server error during the cancellation process.</response>
        [HttpPost("cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Cancel([FromBody] List<string> externalIds)
        {
            if (externalIds == null || !externalIds.Any())
                return BadRequest("The list of TripIds cannot be empty.");

            try
            {
                var count = await _tripService.CancelIntegrationTripsAsync(externalIds, CurrentIntegratorId, CurrentIntegratorName);
                return Ok(new
                {
                    Success = true,
                    Message = $"{count} trips were successfully canceled.",
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves detailed information and current status for your trips.
        /// </summary>
        /// <remarks>
        /// You can filter the results using one of the following methods:
        /// <ul>
        /// <li><b>Query Parameter (date):</b> Returns all your trips for that specific day.</li>
        /// <li><b>Request Body (externalIds):</b> Returns details for specific TripIds.</li>
        /// </ul>
        /// <b>Data Security:</b> This endpoint strictly returns data owned by the authenticated integrator.
        /// </remarks>
        /// <param name="date">Optional date filter (YYYY-MM-DD).</param>
        /// <param name="externalIds">Optional list of specific TripIds to retrieve.</param>
        /// <returns>A list of trip details including current status.</returns>
        /// <response code="200">Returns the list of matching trips.</response>
        /// <response code="400">Validation error if both parameters are missing.</response>
        [HttpPost("details")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDetails([FromQuery] DateTime? date, [FromBody] List<string>? externalIds)
        {
            if (!date.HasValue && (externalIds == null || !externalIds.Any()))
                return BadRequest("Please provide either a date in the query string or a list of TripIds in the request body.");

            try
            {
                var trips = await _tripService.GetIntegrationTripDetailsAsync(date, externalIds, CurrentIntegratorId);

                // Mapping to DTO to expose only necessary fields and protect internal entity structure
                var response = trips.Select(t => new IntegrationTripDto
                {
                    TripId = t.TripId,
                    Status = t.Status,
                    Date = t.Date,
                    FromTime = t.FromTime,
                    ToTime = t.ToTime,
                    CustomerFullName = t.Customer?.FullName,
                    CustomerGender = t.Customer?.Gender,
                    PickupAddress = t.PickupAddress,
                    PickupLatitude = t.PickupLatitude,
                    PickupLongitude = t.PickupLongitude,
                    DropoffAddress = t.DropoffAddress,
                    DropoffLatitude = t.DropoffLatitude,
                    DropoffLongitude = t.DropoffLongitude,
                    Distance = t.Distance ?? 0,
                    PickupComment = t.PickupComment,
                    DropoffComment = t.DropoffComment,
                    SpaceTypeName = t.SpaceType?.Name,
                    FundingSourceName = t.FundingSource?.Name,
                    Type = t.Type
                });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #region Notification Management

        /// <summary>
        /// Exchanges the API Key for a short lived token that opens the notification hub.
        /// </summary>
        /// <remarks>
        /// Connect to <c>/hubs/notifications?access_token={accessToken}</c> and listen for
        /// <c>ReceiveNotification</c>. Notifications about your trips arrive there as they
        /// happen; the endpoints below are the same list, for catching up and for marking
        /// what you already processed.
        ///
        /// <para>
        /// Do not put the API Key in the URL. It opens your whole integration and would be
        /// written to every access log between you and us. That is what this exchange is for.
        /// </para>
        /// </remarks>
        /// <response code="200">Token and the moment it stops working.</response>
        [HttpPost("hub-token")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public IActionResult GetHubToken()
        {
            if (CurrentIntegratorId is not > 0)
                return Unauthorized();

            var token = _hubTokenService.Issue(
                CurrentIntegratorId.Value,
                CurrentIntegratorName);

            return Ok(new
            {
                accessToken = token.AccessToken,
                expiresAtUtc = token.ExpiresAtUtc,
                hubUrl = "/hubs/notifications"
            });
        }

        /// <summary>
        /// Notifications addressed to this integration.
        /// </summary>
        /// <remarks>
        /// Only trips this integration created produce them. Expired notifications are
        /// not returned; see the retention policy for how long each kind lasts.
        /// </remarks>
        [HttpGet("notifications")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetNotifications(CancellationToken cancellationToken)
        {
            if (CurrentIntegratorId is not > 0)
                return Unauthorized();

            // Filtered explicitly. The global query filter on Trip does not cover an API
            // Key request: with no claims, the context treats it as an internal caller.
            var query = new GetRecipientNotificationsQuery(
                UserIdentifierConverter.ToGuid(
                    CurrentIntegratorId.Value,
                    RecipientType.Integration),
                RecipientType.Integration);

            var result = await _getRecipientNotificationsHandler.Handle(
                query,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>Marks one notification as seen.</summary>
        [HttpPost("notifications/{recipientRecordId:guid}/view")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkNotificationViewed(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            if (CurrentIntegratorId is not > 0)
                return Unauthorized();

            await _markViewedHandler.Handle(
                new MarkNotificationViewedCommand(recipientRecordId),
                cancellationToken);

            return NoContent();
        }

        /// <summary>Marks one notification as processed on your side.</summary>
        [HttpPost("notifications/{recipientRecordId:guid}/acknowledge")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> MarkNotificationAcknowledged(
            Guid recipientRecordId,
            CancellationToken cancellationToken)
        {
            if (CurrentIntegratorId is not > 0)
                return Unauthorized();

            await _markAcknowledgedHandler.Handle(
                new MarkNotificationAcknowledgedCommand(recipientRecordId),
                cancellationToken);

            return NoContent();
        }

        #endregion
    }
}