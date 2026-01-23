using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services;
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
        public async Task<ActionResult<TripHistory>> PostHistory(TripHistory history)
        {
            if (history.ChangeDate == default)
                history.ChangeDate = DateTime.Now;

            var result = await _service.PostHistory(history);

            return Ok(result);
        }
    }
}
