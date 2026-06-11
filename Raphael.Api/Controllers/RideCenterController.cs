using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Attributes;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using System.Net.Mime;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/integration/ride-center")]
    [AllowAnonymous] // Ignore JWT Global
    [RideCenterApiKey] // Custom attribute for API Key security
    [Produces(MediaTypeNames.Application.Json)]
    public class RideCenterController : ControllerBase
    {
        private readonly ITripService _tripService;

        public RideCenterController(ITripService tripService)
        {
            _tripService = tripService;
        }

        /// <summary>
        /// Synchronizes a list of trips from Ryde Central.
        /// </summary>
        /// <remarks>
        /// This endpoint performs an "Upsert" (Update or Insert) operation:
        /// - If the TripId exists, the trip is updated.
        /// - If the TripId does not exist, a new trip is created.
        /// - Customers are identified by RiderId (or FullName + Phone if RiderId is missing).
        /// - SpaceTypes and FundingSources are identified by their Name.
        /// </remarks>
        /// <param name="trips">List of trip objects to be synchronized.</param>
        /// <returns>A summary of the processed trips.</returns>
        /// <response code="200">The synchronization was successful.</response>
        /// <response code="400">The request body is invalid or empty.</response>
        /// <response code="401">Unauthorized. API Key is missing or invalid.</response>
        /// <response code="500">Internal server error during processing.</response>
        [HttpPost("sync-batch")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SyncBatch([FromBody] List<RideCenterTripDto> trips)
        {
            if (trips == null || !trips.Any())
            {
                return BadRequest("The trip list cannot be empty.");
            }

            try
            {
                // This method should be implemented in your TripService
                var processedTripIds = await _tripService.UpsertRideCenterTripsAsync(trips);

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
                // Log the exception here
                return StatusCode(500, $"An error occurred during synchronization: {ex.Message}");
            }
        }

        /// <summary>
        /// Synchronizes a single trip from Ryde Central.
        /// </summary>
        /// <param name="trip">The trip object to be synchronized.</param>
        /// <response code="200">The trip was successfully synchronized.</response>
        /// <response code="401">Unauthorized.</response>
        [HttpPost("sync-single")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SyncSingle([FromBody] RideCenterTripDto trip)
        {
            if (trip == null) return BadRequest("Trip data is required.");

            return await SyncBatch(new List<RideCenterTripDto> { trip });
        }
    }
}