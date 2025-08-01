using Raphael.TripsService.Data;
using Raphael.TripsService.DTOs;
using Raphael.TripsService.Models;
using Raphael.TripsService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Raphael.TripsService.Controllers
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
        public async Task<IActionResult> Create([FromBody] TripCreateDto dto)
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

