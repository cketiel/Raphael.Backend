using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using System.Text.RegularExpressions;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SchedulesController : ControllerBase
    {
        private readonly IScheduleService _scheduleService;

        public SchedulesController(IScheduleService scheduleService)
        {
            _scheduleService = scheduleService;
        }

        [HttpGet("by-run-login")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedulesByRunLogin([FromQuery] string runLogin, [FromQuery] DateTime date)
        {
            var schedules = await _scheduleService.GetSchedulesByRunLoginAndDateAsync(runLogin, date);
            return Ok(schedules);
        }

        [HttpGet("driver/pending")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetPendingSchedulesForDriver([FromQuery] string runLogin, [FromQuery] DateTime date)
        {
            var schedules = await _scheduleService.GetPendingSchedulesForDriverAsync(runLogin, date);
            return Ok(schedules);
        }

        [HttpGet("by-route")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetSchedules([FromQuery] int vehicleRouteId, [FromQuery] DateTime date)
        {
            var schedules = await _scheduleService.GetSchedulesByRouteAndDateAsync(vehicleRouteId, date);
            return Ok(schedules);
        }

        [HttpGet("unscheduled")]
        public async Task<ActionResult<IEnumerable<UnscheduledTripDto>>> GetUnscheduledTrips([FromQuery] DateTime date)
        {
            var trips = await _scheduleService.GetUnscheduledTripsByDateAsync(date);
            return Ok(trips);
        }

        [HttpPost("route")]
        public async Task<IActionResult> RouteTrips([FromBody] RouteTripRequest request)
        {
            try
            {
                await _scheduleService.RouteTripAsync(request);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }


        [HttpPost("cancel-route")]
        public async Task<IActionResult> CancelRoute([FromBody] CancelRouteRequest request)
        {
            try
            {
                await _scheduleService.CancelRouteForTripAsync(request.ScheduleId);
                return Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] ScheduleDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var success = await _scheduleService.UpdateAsync(id, dto);
            if (!success)
            {
                return NotFound($"Could not update because the schedule with ID {id} was not found.");
            }
            // 204 NoContent is the standard response for a successful PUT that returns no content.
            return NoContent();
        }

        [HttpPost("{id}/signature")]
        public async Task<IActionResult> UploadSignature(int id, [FromBody] SignatureUploadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SignatureBase64))
            {
                return BadRequest("Signature data is required.");
            }

            try
            {
                // Convert Base64 string back to a byte array
                var signatureBytes = Convert.FromBase64String(dto.SignatureBase64);
                var success = await _scheduleService.SaveSignatureAsync(id, signatureBytes);

                if (!success)
                {
                    return NotFound($"Schedule with ID {id} not found.");
                }
                return Ok();
            }
            catch (FormatException)
            {
                return BadRequest("Invalid Base64 string for signature.");
            }
        }

        [HttpGet("{id}/signature")]
        public async Task<IActionResult> GetSignature(int id)
        {
            var signatureBytes = await _scheduleService.GetSignatureAsync(id);
            if (signatureBytes == null)
            {
                return NotFound();
            }

            // We return the image as a file, the browser/client will know how to display it
            return File(signatureBytes, "image/png");
        }

        [HttpGet("driver/future")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetFutureSchedulesForDriver([FromQuery] string runLogin)
        {
            var schedules = await _scheduleService.GetFutureSchedulesForDriverAsync(runLogin);
            return Ok(schedules);
        }

        [HttpGet("history/{runLogin}/{date}")]
        public async Task<ActionResult<IEnumerable<ScheduleHistoryDto>>> GetHistory(string runLogin, DateTime date)
        {
            var history = await _scheduleService.GetScheduleHistoryAsync(runLogin, date);
            return Ok(history);
        }

        [HttpGet("history/count/{runLogin}/{date}")]
        public async Task<ActionResult<int>> GetHistoryCount(string runLogin, DateTime date)
        {
            var count = await _scheduleService.GetScheduleHistoryCountAsync(runLogin, date);
            return Ok(count);
        }

        [HttpPut("trip/{tripId}/contact-phone")]
        public async Task<IActionResult> UpdateContactPhoneNumber(int tripId, [FromBody] UpdatePhoneDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PhoneNumber))
            {
                return BadRequest("A valid phone number is required.");
            }

            var success = await _scheduleService.UpdateContactPhoneNumberAsync(tripId, dto.PhoneNumber);

            if (!success)
            {
                return NotFound($"Trip with ID {tripId} or its associated customer was not found.");
            }

            // 204 NoContent is the standard response for a successful PUT that returns no content.
            return NoContent();
        }

        [HttpGet("reports/production")]
        public async Task<ActionResult<IEnumerable<ProductionReportRowDto>>> GetProductionReport([FromQuery] DateTime date, [FromQuery] int? fundingSourceId)
        {
            var reportData = await _scheduleService.GetProductionReportDataAsync(date, fundingSourceId);
            return Ok(reportData);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduleDto>> GetById(int id)
        {
            var schedule = await _scheduleService.GetByIdAsync(id);

            if (schedule == null)
            {
                return NotFound($"Schedule with ID {id} not found.");
            }

            return Ok(schedule);
        }

        [EnableRateLimiting("public-api")]  // This endpoint is public and can be accessed without authentication, so we apply rate limiting to prevent abuse. (Protect the public endpoint)
        [HttpGet("patient-eta")]
        public async Task<ActionResult<IEnumerable<ScheduleDto>>> GetPatientETA([FromQuery] string patientName)
        {
            if (string.IsNullOrWhiteSpace(patientName))
            {
                return BadRequest("The patient's name is required.");
            }

            patientName = patientName.Trim();

            // Avoid large strings
            if (patientName.Length > 100)
                return BadRequest("Invalid patient name.");

            // Avoid rare characters and possible injection attempts
            if (!Regex.IsMatch(patientName, @"^[a-zA-Z\s\.\-']+$"))
                return BadRequest("Invalid characters.");

            // We use the current server date
            DateTime toDay = DateTime.Today;
            DateTime searchDate = DateTime.SpecifyKind(toDay, DateTimeKind.Unspecified); // force UTC conversion not to be applied, tells the system: "Don't touch the time, send it as is."
            var etas = await _scheduleService.GetPatientETAsByNameAsync(patientName, searchDate);

            if (!etas.Any())
            {
                return NotFound("No trips scheduled for today were found with that patient name.");
            }

            return Ok(etas);
        }

    }
}

