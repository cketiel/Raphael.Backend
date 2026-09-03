using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Realtime;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Realtime;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;

namespace Raphael.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GpsController : ControllerBase
    {
        private readonly IGpsService _gpsService;
        private readonly IDispatchBroadcaster _board;
        private readonly IOperationClock _clock;
        private readonly ICurrentUserService _currentUser;

        public GpsController(
            IGpsService gpsService,
            IDispatchBroadcaster board,
            IOperationClock clock,
            ICurrentUserService currentUser)
        {
            _gpsService = gpsService;
            _board = board;
            _clock = clock;
            _currentUser = currentUser;
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

                // Pushed to whoever has that route on screen, instead of every open tab asking
                // "where is it now" every five seconds. The driver's app reports about every
                // thirty seconds, so this is one message per vehicle per half minute, delivered
                // only to the screens actually watching that route.
                //
                // The operating day comes from the clock and not from the fix's own date: a
                // report taken at two in the morning UTC belongs to the previous evening here,
                // and the screens are grouped by the day the office is working.
                await _board.VehiclePositionAsync(
                    new VehiclePositionMessage
                    {
                        VehicleRouteId = gpsDataDto.IdVehicleRoute,
                        Latitude = gpsDataDto.Latitude,
                        Longitude = gpsDataDto.Longitude,
                        Speed = gpsDataDto.Speed,
                        Direction = gpsDataDto.Direction,
                        AtUtc = DateTime.UtcNow
                    },
                    _clock.TodayFor(_currentUser.ProviderId));

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
