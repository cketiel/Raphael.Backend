using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Attributes;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
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

        /// <summary>
        /// Retrieves the Integrator ID stored in the HttpContext by the security filter.
        /// </summary>
        private int CurrentIntegratorId => (int)HttpContext.Items["IntegratorId"]!;

        public IntegrationController(ITripService tripService)
        {
            _tripService = tripService;
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
        /// <response code="200">Synchronization completed successfully.</response>
        /// <response code="400">The request is empty or contains malformed data.</response>
        /// <response code="401">Unauthorized. API Key is invalid or missing.</response>
        /// <response code="500">Internal server error during data processing.</response>
        [HttpPost("sync")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //To handle files (PDF/Word), the request format was changed from JSON to Multipart Form-Data. This was necessary because the JSON protocol is not efficient for sending binary files.
        public async Task<IActionResult> Sync([FromForm] List<IntegrationTripDto> trips) // Critical Change: Changed [FromBody] to [FromForm]. When files are sent, the data does not travel as a flat JSON in the body, but as a form with parts (multipart).
        {
            if (trips == null || !trips.Any())
            {
                return BadRequest("The trip list cannot be empty.");
            }

            try
            {               
                var processedTripIds = await _tripService.UpsertIntegrationTripsAsync(trips, CurrentIntegratorId);

                return Ok(new
                {
                    Success = true,
                    Message = "Synchronization completed successfully.",
                    ProcessedCount = processedTripIds.Count,
                    Timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {               
                return StatusCode(500, $"An error occurred during synchronization: {ex.Message}");
            }
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
                var count = await _tripService.CancelIntegrationTripsAsync(externalIds, CurrentIntegratorId);
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
                    PickupAddress = t.PickupAddress,
                    DropoffAddress = t.DropoffAddress,
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
    }
}