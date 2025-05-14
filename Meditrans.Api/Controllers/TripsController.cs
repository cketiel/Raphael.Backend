
using Meditrans.Shared.DTOs;
using Meditrans.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Meditrans.Shared.Entities;

namespace Meditrans.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ITripService _tripService;

        public TripsController(ITripService tripService)
        {
            _tripService = tripService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var trips = await _tripService.GetAllAsync();
            return Ok(trips);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var trip = await _tripService.GetByIdAsync(id);
            return trip == null ? NotFound() : Ok(trip);
        }

        [HttpPost]
        public async Task<ActionResult<Trip>> Create([FromBody] TripCreateDto dto)
        {
            try
            {
                var createdTrip = await _tripService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = createdTrip.Id }, createdTrip);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, "Database error while creating trip");
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create2([FromBody] TripCreateDto dto)
        {
            var result = await _tripService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] TripUpdateDto dto)
        {
            var updated = await _tripService.UpdateAsync(id, dto);
            return updated ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _tripService.DeleteAsync(id);
            return deleted ? NoContent() : NotFound();
        }
    }

}
