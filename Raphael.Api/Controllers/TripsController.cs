
using Raphael.Shared.DTOs;
using Raphael.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Shared.Entities;

namespace Raphael.Api.Controllers
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
            /*return Ok(new
            {
                Success = true,
                Data = trips,
                Count = trips.Count
            });*/

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
            if (!ModelState.IsValid)
            {
                // Returns specific validation errors
                return BadRequest(new
                {
                    Message = "Validation errors occurred",
                    Errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList()
                });
            }
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
                // Capture the internal error (inner exception) which has more details
                var innerExceptionMessage = ex.InnerException?.Message;
                return StatusCode(500, $"Database error while creating trip: {innerExceptionMessage}");                
            }
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

        [HttpGet("date/{date}")]
        public async Task<IActionResult> GetByDate(DateTime date)
        {
            var trips = await _tripService.GetByDateAsync(date);
            return Ok(trips);
        }

        [HttpGet("date-range")]
        public async Task<IActionResult> GetByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            try
            {
                // Basic validation
                if (startDate > endDate)
                {
                    return BadRequest("The start date cannot be greater than the end date");
                }

                var trips = await _tripService.GetByDateRangeAsync(startDate, endDate);
                return Ok(trips);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpGet("paginated")]
        public async Task<IActionResult> GetAllPaginated(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Basic parameter validation
            if (pageNumber < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            try
            {
                var (trips, totalCount) = await _tripService.GetAllAsync(pageNumber, pageSize);

                // Calculate pagination metadata
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    Success = true,
                    Data = trips,
                    Pagination = new
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = pageNumber > 1,
                        HasNext = pageNumber < totalPages
                    }
                });
            }
            catch (Exception ex)
            {
                // Log the exception 
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Gets paginated trips for a specific date
        /// </summary>
        /// <param name="date">Date of trips to consult (format: YYYY-MM-DD)</param>
        /// <param name="pageNumber">Page number (default 1)</param>
        /// <param name="pageSize">Page size (default 20, maximum 100)</param>
        /// <returns>Paginated list of trips for the specified date</returns>
        [HttpGet("date/{date}/paginated")]
        public async Task<IActionResult> GetByDatePaginated(
            DateTime date,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            // Basic parameter validation
            if (pageNumber < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            try
            {
                var (trips, totalCount) = await _tripService.GetByDatePaginatedAsync(date, pageNumber, pageSize);

                // Calculate pagination metadata
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    Success = true,
                    Data = trips,
                    Pagination = new
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = pageNumber > 1,
                        HasNext = pageNumber < totalPages
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception 
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        /// <summary>
        /// Gets paginated trips within a date range
        /// </summary>
        /// <param name="startDate">Start date (format: YYYY-MM-DD)</param>
        /// <param name="endDate">End date (format: YYYY-MM-DD)</param>
        /// <param name="pageNumber">Page number (default 1)</param>
        /// <param name="pageSize">Page size (default 20, maximum 100)</param>
        [HttpGet("date-range/paginated")]
        public async Task<IActionResult> GetByDateRangePaginated(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {           
            if (pageNumber < 1)
            {
                return BadRequest("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                return BadRequest("Page size must be between 1 and 100");
            }

            try
            {
                var (trips, totalCount) = await _tripService.GetByDateRangePaginatedAsync(startDate, endDate, pageNumber, pageSize);
              
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                return Ok(new
                {
                    Success = true,
                    Data = trips,
                    Pagination = new
                    {
                        CurrentPage = pageNumber,
                        PageSize = pageSize,
                        TotalCount = totalCount,
                        TotalPages = totalPages,
                        HasPrevious = pageNumber > 1,
                        HasNext = pageNumber < totalPages
                    },
                    DateRange = new
                    {
                        StartDate = startDate.ToString("yyyy-MM-dd"),
                        EndDate = endDate.ToString("yyyy-MM-dd")
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                // Log the exception 
                return StatusCode(500, "An error occurred while processing the request");
            }
        }

        [HttpPost("{id}/cancel")] // Ruta: api/trips/123/cancel
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CancelTrip(int id)
        {
            var success = await _tripService.CancelAsync(id);

            if (!success)
            {
                return NotFound(new { Message = $"Trip with ID {id} not found or cannot be cancelled." });
            }

            return NoContent(); // Success
        }

        [HttpPatch("{id}/dispatch-update")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateFromDispatch(int id, [FromBody] TripDispatchUpdateDto dto)
        {
            if (dto == null)
            {
                return BadRequest();
            }

            var success = await _tripService.UpdateFromDispatchAsync(id, dto);

            if (!success)
            {
                return NotFound(new { Message = $"Trip with ID {id} not found." });
            }

            return NoContent();
        }

    }// end class
}

