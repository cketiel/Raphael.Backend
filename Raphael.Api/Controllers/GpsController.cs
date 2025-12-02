using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GpsController : ControllerBase
    {
        private readonly IGpsService _gpsService;

        public GpsController(IGpsService gpsService)
        {
            _gpsService = gpsService;
        }

        // POST: api/Gps
        [HttpPost]
        public async Task<IActionResult> PostGpsData([FromBody] GpsDataDto gpsDataDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                await _gpsService.SaveGpsDataAsync(gpsDataDto);
                
                return Ok("GPS data saved successfully.");
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, "An internal error occurred while saving GPS data.");
            }
        }

        // GET: api/Gps/latest/{vehicleRouteId}
        [HttpGet("latest/{vehicleRouteId}")]
        public async Task<ActionResult<GpsDataDto>> GetLatestGpsData(int vehicleRouteId)
        {
            try
            {
                var gpsData = await _gpsService.GetLatestGpsDataAsync(vehicleRouteId);

                if (gpsData == null)
                {
                    return NotFound($"No GPS data found for vehicle route ID {vehicleRouteId}.");
                }

                return Ok(gpsData);
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                return StatusCode(500, "An internal error occurred while retrieving GPS data.");
            }
        }

        // GET: api/Gps/reports/history?vehicleRouteId=1&date=2025-12-01
        [HttpGet("reports/history")]
        public async Task<ActionResult<IEnumerable<GpsDataDto>>> GetGpsHistoryReport(
            [FromQuery] int vehicleRouteId, [FromQuery] DateTime date)
        {
            try
            {
                var gpsHistory = await _gpsService.GetGpsHistoryForReportAsync(vehicleRouteId, date);
                // It's perfectly fine to return an empty list if no data is found for that day.
                return Ok(gpsHistory);
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                return StatusCode(500, "An internal error occurred while retrieving GPS history.");
            }
        }

    }
}
