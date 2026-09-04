
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TripsController : ControllerBase
    {
        private readonly ITripService _tripService;
        private readonly ICurrentUserService _currentUserService;

        public TripsController(ITripService tripService, ICurrentUserService currentUserService)
        {
            _tripService = tripService;
            _currentUserService = currentUserService;
        }

        [HttpPut("update-types")]
        public async Task<IActionResult> UpdateTripTypes([FromBody] List<TripTypeUpdateDto> updates)
        {
            if (updates == null || !updates.Any())
                return BadRequest("No data provided.");

            try
            {
                // Llamada al servicio para actualizar
                await _tripService.UpdateTripTypesAsync(updates);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// Stores a chunk of a broker's CSV file in one request.
        /// </summary>
        /// <remarks>
        /// Answers per row, so read <i>Results</i> rather than the status code alone: a rejected
        /// row stores nothing and does not stop the rest of the chunk. Each carries an
        /// <i>ErrorCode</i>, a message saying what to fix in the file, a <i>Retryable</i> flag and
        /// a <i>CorrelationId</i> for support.
        ///
        /// <para>
        /// Re-sending a chunk is safe: rows are matched on the broker's own TripId, so a trip
        /// that already went in is updated rather than duplicated. That is what lets the client
        /// retry a chunk the shared host refused without the office having to work out which
        /// half of the file arrived.
        /// </para>
        /// </remarks>
        /// <response code="200">Every row in the chunk was stored.</response>
        /// <response code="207">Some rows were stored and some were rejected. See Results.</response>
        /// <response code="400">The chunk is empty, or names a funding source that does not exist.</response>
        /// <response code="422">Every row in the chunk was rejected. See Results.</response>
        [HttpPost("import")]
        [Authorize]
        [ProducesResponseType(typeof(TripImportResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(TripImportResultDto), StatusCodes.Status207MultiStatus)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(TripImportResultDto), StatusCodes.Status422UnprocessableEntity)]
        public async Task<IActionResult> Import([FromBody] TripImportRequestDto request)
        {
            if (request?.Items == null || request.Items.Count == 0)
            {
                return BadRequest("The trip list cannot be empty.");
            }

            // No catch on the general case, on purpose. Failures that belong to one row are
            // translated and reported inside the chunk; anything that escapes is a fault of
            // ours, and the global handler logs it in full and answers with ProblemDetails.
            try
            {
                var result = await _tripService.ImportTripsAsync(request);

                if (result.FailedCount == 0)
                {
                    return Ok(result);
                }

                return result.CreatedCount + result.UpdatedCount == 0
                    ? UnprocessableEntity(result)
                    : StatusCode(StatusCodes.Status207MultiStatus, result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
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
                // This specifically catches the duplicate error sent from the service
                return Conflict(new { Message = ex.Message }); // Error Code 409: Using Conflict() in the controller helps the Frontend know that it is not a programming error (500), but a duplicate data problem, allowing it to display an alert message to the user.
                //return Conflict(ex.Message);
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
            try
            {
                var updated = await _tripService.UpdateAsync(id, dto);
                return updated ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex)
            {
                // This specifically catches the duplicate error sent from the service
                return Conflict(new { Message = ex.Message }); // Error Code 409: Using Conflict() in the controller helps the Frontend know that it is not a programming error (500), but a duplicate data problem, allowing it to display an alert message to the user.
            }
            catch (DbUpdateException ex)
            {
                // This captures foreign key errors, null data, etc., in the database.
                var innerExceptionMessage = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, $"Database error while updating trip: {innerExceptionMessage}");
            }
            catch (Exception ex)
            {
                // This captures general errors
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }

            //var updated = await _tripService.UpdateAsync(id, dto);
            //return updated ? NoContent() : NotFound();
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

        // POST: api/Trips/{id}/cancel-by-driver
        [HttpPost("{id}/cancel-by-driver")]
        public async Task<IActionResult> CancelByDriver(int id, [FromBody] DriverCancelTripDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string driverName = _currentUserService.UserName ?? "DriverUser";
            var success = await _tripService.CancelByDriverAsync(id, dto.Reason, driverName);
            if (!success)
            {
                // It could be because the trip was not found or was already cancelled/ended
                return NotFound($"Trip with ID {id} not found or cannot be cancelled.");
            }

            return Ok("Trip successfully cancelled by driver.");
        }

        [HttpPost("{id}/uncancel")] // Ruta: api/trips/123/uncancel
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UncancelTrip(int id)
        {
            try
            {
                var success = await _tripService.UncancelAsync(id);

                if (!success)
                {
                    return NotFound(new { Message = $"Trip with ID {id} not found or cannot be restored." });
                }

                return NoContent(); // Success
            }
            catch (InvalidOperationException ex)
            {
                // If when trying to uncancel it is detected that there is already another active equal
                return Conflict(new { Message = ex.Message }); // Error Code 409: Using Conflict() in the controller helps the Frontend know that it is not a programming error (500), but a duplicate data problem, allowing it to display an alert message to the user.
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// The office activates a Will Call because the patient rang instead of using the app.
        /// </summary>
        /// <remarks>
        /// ⚠️ One of the only two endpoints allowed to move <c>Trip.WillCall</c>. From here
        /// the office has one hour to get a vehicle to the patient.
        /// </remarks>
        [HttpPost("{id}/will-call/activate")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateWillCall(int id, [FromBody] WillCallUpdateDto? dto)
        {
            try
            {
                var success = await _tripService.ActivateWillCallAsync(id, dto?.FromTime);

                if (!success)
                {
                    return NotFound(new { Message = $"Trip with ID {id} not found, cancelled, or not a Will Call." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
        }

        /// <summary>
        /// The trip goes back to waiting for the patient to say they are ready.
        /// </summary>
        [HttpPost("{id}/will-call/revert")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RevertToWillCall(int id, [FromBody] WillCallUpdateDto? dto)
        {
            try
            {
                var success = await _tripService.RevertToWillCallAsync(id, dto?.FromTime);

                if (!success)
                {
                    return NotFound(new { Message = $"Trip with ID {id} not found, cancelled, or already a Will Call." });
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal error: {ex.Message}");
            }
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

        [HttpPatch("{id}/assign-run")]
        public async Task<IActionResult> AssignRun(int id, [FromBody] int? vehicleRouteId)
        {
            if (vehicleRouteId == null)
            {
                return BadRequest();
            }

            var success = await _tripService.AssignRunAsync(id, vehicleRouteId);

            if (!success)
            {
                return NotFound(new { Message = $"Trip with ID {id} not found." });
            }

            return NoContent();
           
        }

        [HttpPost("{id}/start")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> StartTrip(int id, [FromQuery] TimeSpan? travel)
        {
            try
            {
                var success = await _tripService.StartTripAsync(id, travel);

                if (!success)
                {
                    return NotFound(new
                    {
                        Message = $"Trip with ID {id} not found or cannot be started."
                    });
                }

                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    $"Internal server error: {ex.Message}");
            }
        }

    }// end class
}

