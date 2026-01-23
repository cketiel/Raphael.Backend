using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;

namespace Raphael.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripHistoryController : ControllerBase
    {
        private readonly ITripHistoryService _service;

        public TripHistoryController(ITripHistoryService service)
        {
            _service = service;
        }

        // GET: api/TripHistory/5
        [HttpGet("{tripId}")]
        public async Task<ActionResult<IEnumerable<TripHistory>>> GetHistoryByTrip(int tripId)
        {
            var history = await _service.GetHistoryByTrip(tripId);
            return Ok(history);
            
        }

        // POST: api/TripHistory
        [HttpPost]
        public async Task<ActionResult<TripHistory>> PostHistory(TripHistoryCreateDto dto)
        {           
            var history = new TripHistory
            {
                TripId = dto.TripId,
                User = dto.User,
                Field = dto.Field,
                PriorValue = dto.PriorValue,
                NewValue = dto.NewValue,
                ChangeDate = dto.ChangeDate ?? DateTime.Now
            };

            var result = await _service.PostHistory(history);
            return Ok(result);
        }
    }
}
