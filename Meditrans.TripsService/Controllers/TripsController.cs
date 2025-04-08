using Meditrans.TripsService.Data;
using Meditrans.TripsService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.TripsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly TripsDbContext _context;

        public TripsController(TripsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Trip>>> GetAll()
        {
            return Ok(await _context.Trips.ToListAsync());
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Trip>> GetById(Guid id)
        {
            var trip = await _context.Trips.FindAsync(id);
            return trip == null ? NotFound() : Ok(trip);
        }

        [HttpPost]
        public async Task<ActionResult<Trip>> Create([FromBody] Trip trip)
        {
            trip.Id = Guid.NewGuid();
            _context.Trips.Add(trip);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetById), new { id = trip.Id }, trip);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] Trip updated)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            trip.PatientName = updated.PatientName;
            trip.Date = updated.Date;
            trip.FromTime = updated.FromTime;
            trip.ToTime = updated.ToTime;
            trip.PickupAddress = updated.PickupAddress;
            trip.DropoffAddress = updated.DropoffAddress;
            trip.Day = updated.Day;
            trip.SpaceType = updated.SpaceType;
            trip.PickupNote = updated.PickupNote;
            trip.IsCancelled = updated.IsCancelled;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null) return NotFound();

            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
