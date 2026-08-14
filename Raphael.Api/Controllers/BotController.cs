using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using System.Text.RegularExpressions;
using System.Net.Mime;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// Controller dedicated to Customer Service Bot integrations.
    /// Protected by API Key.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Bypasses Global JWT; security is handled by ApiKeyAuthFilter
    [ServiceFilter(typeof(ApiKeyAuthFilter))]
    [Produces(MediaTypeNames.Application.Json)]
    public class BotController : ControllerBase
    {
        private readonly IBotService _botService;
        public BotController(IBotService botService)
        {
            _botService = botService;
        }

        /// <summary>
        /// Activates the "Will Call" status for a specific trip.
        /// </summary>
        /// <param name="request">Object containing the Trip Number.</param>
        /// <returns>A success flag and a status message.</returns>
        [HttpPost("activate-willcall")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ActivateWillCall([FromBody] BotTripRequest request)
        {
            if (string.IsNullOrEmpty(request.TripNumber))
                return BadRequest("TripNumber is required.");

            string message = await _botService.ActivateWillCallAsync(request.TripNumber);
            return Ok(new { Success = message == "SUCCESS", Message = message });         
        }

        /// <summary>
        /// Cancels an existing trip.
        /// </summary>
        /// <param name="request">Object containing the Trip Number.</param>
        /// <returns>A success flag and a status message.</returns>
        [HttpPost("cancel-trip")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CancelTrip([FromBody] BotTripRequest request)
        {
            if (string.IsNullOrEmpty(request.TripNumber))
                return BadRequest("TripNumber is required.");

            string message = await _botService.CancelTripAsync(request.TripNumber);
            return Ok(new { Success = message == "SUCCESS", Message = message });
        }

        /// <summary>
        /// Retrieves the Estimated Time of Arrival (ETA) using a Trip Number.
        /// </summary>
        /// <param name="tripNumber">The unique trip identifier.</param>
        [HttpGet("eta")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetEta([FromQuery] string tripNumber)
        {
            if (string.IsNullOrEmpty(tripNumber))
                return BadRequest("TripNumber is required.");

            var eta = await _botService.GetEtaAsync(tripNumber);
            return Ok(new
            {
                Success = eta.HasValue,
                Message = eta.HasValue ? "ETA_AVAILABLE" : "ETA_NOT_AVAILABLE",
                Eta = eta?.ToString(@"hh\:mm") ?? "N/A"
            });
        }

        /// <summary>
        /// Retrieves the ETA by searching for trip number or patient profile (Name, Phone, and Date).
        /// </summary>
        /// <param name="patientName">Optional. Full name of the patient.</param>
        /// <param name="phone">Optional. Contact phone number.</param>
        /// <param name="date">Optional. Date of the service.</param>
        /// <param name="tripNumber">Optional. Unique trip identifier.</param>
        [HttpGet("patient-eta")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetPatientETA(
            [FromQuery] string? patientName,
            [FromQuery] string? phone,
            [FromQuery] DateTime? date,
            [FromQuery] string? tripNumber)
        {
            bool isTripSearch = !string.IsNullOrWhiteSpace(tripNumber);
            bool isProfileSearch = !string.IsNullOrWhiteSpace(patientName) && !string.IsNullOrWhiteSpace(phone) && date.HasValue;

            if (!isTripSearch && !isProfileSearch)
            {
                return BadRequest("Search criteria missing: Provide tripNumber OR (patientName, phone, and date).");
            }

            if (isProfileSearch)
            {
                patientName = patientName!.Trim();
                phone = Regex.Replace(phone!, @"[^\d]", "");

                if (patientName.Length > 100 || phone.Length > 20)
                    return BadRequest("Invalid input length.");

                if (!Regex.IsMatch(patientName, @"^[a-zA-Z\s\.\-']+$"))
                    return BadRequest("Patient name contains invalid characters.");
            }

            var eta = await _botService.GetEtaAsync(patientName, phone, date, tripNumber);
            bool success = eta.HasValue;

            return Ok(new
            {
                Success = success,
                Message = success ? "ETA_AVAILABLE" : "ETA_NOT_AVAILABLE",
                Eta = eta?.ToString(@"hh\:mm") ?? "N/A"
            });
        }
    }

    /// <summary>
    /// Data Transfer Object for Bot Trip requests.
    /// </summary>
    public class BotTripRequest
    {
        /// <summary>
        /// The unique alphanumeric Trip Identifier.
        /// </summary>
        public string TripNumber { get; set; } = string.Empty;
    }
}