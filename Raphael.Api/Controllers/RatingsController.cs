using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RatingsController : ControllerBase
    {
        private readonly IRatingService _ratingService;

        public RatingsController(IRatingService ratingService)
        {
            _ratingService = ratingService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var rating = await _ratingService.GetByIdAsync(id);
            return rating == null ? NotFound() : Ok(rating);
        }

        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetByDriver(int driverId)
        {
            return Ok(await _ratingService.GetByDriverIdAsync(driverId));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RatingCreateDto dto)
        {
            var result = await _ratingService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] RatingUpdateDto dto)
        {
            var success = await _ratingService.UpdateAsync(id, dto.Score, dto.Comment);
            return success ? NoContent() : NotFound();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _ratingService.DeleteAsync(id);
            return success ? NoContent() : NotFound();
        }
    }

    public class RatingUpdateDto
    {
        public double Score { get; set; }
        public string? Comment { get; set; }
    }
}