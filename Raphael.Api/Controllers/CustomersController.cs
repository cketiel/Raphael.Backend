using Raphael.Api.Services;
using Raphael.Shared.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;
using FluentValidation.AspNetCore;
using Raphael.Shared.DTOs;
using Microsoft.Data.SqlClient;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// Management of customer entities
    /// </summary>
    //[Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    public class CustomersController : ControllerBase
    {
        private readonly CustomerService _customerService;
        private readonly ILogger<CustomersController> _logger;
        private readonly IValidator<CustomerCreateDto> _validator;

        public CustomersController(
            CustomerService customerService,
            ILogger<CustomersController> logger,
            IValidator<CustomerCreateDto> validator)
        {
            _customerService = customerService;
            _logger = logger;
            _validator = validator;
        }

        // GET: api/Customers
        /// <summary>
        /// Retrieve a complete list of all customers in the system
        /// </summary>
        /// <response code="200">Successfully retrieved customer list</response>
        /// <response code="401">Unauthorized access</response>
        /// <response code="500">Internal server error</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<CustomerResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<CustomerResponseDto>>> GetAll()
        {
            try
            {
                _logger.LogInformation("Retrieving all customers");
                //_logger.LogInformation("Initiating customer data retrieval");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var customers = await _customerService.GetAllAsync();
                stopwatch.Stop();

                _logger.LogInformation("Successfully retrieved {CustomerCount} customers in {ElapsedMilliseconds}ms",
                customers.Count,
                stopwatch.ElapsedMilliseconds);

                //_logger.LogDebug("Customer data: {CustomerData}", customers.Select(c => new { c.Id, c.FullName }).Take(5));

                return Ok(customers);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error while retrieving customers");              
                return Problem(
                    title: "Database Error",
                    detail: "Failed to retrieve data",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error retrieving customers");
                //_logger.LogCritical(ex, "Critical failure in customer retrieval: {ErrorTrace}",ex.StackTrace);
                return Problem(
                    title: "Server Error",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // GET: api/Customers/5
        /// <summary>
        /// Get customer by ID
        /// </summary>
        /// <param name="id">Customer identifier</param>
        /// <response code="200">Customer found</response>
        /// <response code="404">Customer not found</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CustomerResponseDto>> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving customer with ID: {CustomerId}", id);
                var customer = await _customerService.GetByIdAsync(id);

                return customer == null
                    ? NotFound(CreateProblemDetails("Not Found", $"Customer {id} not found", 404))
                    : Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer {CustomerId}", id);
                return Problem(
                    title: "Server Error",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // POST: api/Customers
        /// <summary>
        /// Create new customer
        /// </summary>
        /// <param name="dto">Customer data</param>
        /// <response code="201">Customer created</response>
        /// <response code="400">Validation error</response>
        /// <response code="500">Internal server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CustomerResponseDto>> Create([FromBody] CustomerCreateDto dto)
        {
            _logger.LogInformation("Initiating customer creation process");

            // First validation layer (DataAnnotations)
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for customer creation");
                //_logger.LogWarning("ModelState validation failed with {ErrorCount} errors", ModelState.ErrorCount);
                return ValidationProblem(ModelState);
            }

            // Second validation layer (FluentValidation)
            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for customer creation");
                //_logger.LogWarning("Business validation failed with {ErrorCount} errors", validationResult.Errors.Count);
                validationResult.AddToModelState(ModelState);
                return ValidationProblem(ModelState);
            }

            try
            {
                _logger.LogDebug("Attempting to persist customer in database");
                var createdCustomer = await _customerService.CreateAsync(dto);
                //_logger.LogInformation("Created customer with ID: {CustomerId}", createdCustomer.Id);
                _logger.LogInformation("Customer created successfully. Generated ID: {CustomerId}", createdCustomer.Id);

                return CreatedAtAction(
                    nameof(GetById),
                    new { id = createdCustomer.Id },
                    createdCustomer);
            }
            // General Validation
            /*catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true)
            {
                _logger.LogError(ex, "Duplicate entry error creating customer");
                return Conflict(CreateProblemDetails(
                    "Duplicate Entry",
                    "Customer with similar unique data already exists",
                    StatusCodes.Status409Conflict));
            }*/
            // Specific validations for SQL Server
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogError(ex, "Duplicate entry detected for customer data: {Email} | {ClientCode}", dto.Email, dto.ClientCode);
                return Conflict(CreateProblemDetails(
                    title: "Duplicate Entry",
                    detail: "Customer data conflicts with existing records",
                    statusCode: StatusCodes.Status409Conflict));
            }
            catch (DbUpdateException ex) 
            {
                _logger.LogError(ex, "Database error creating customer");
                //_logger.LogError(ex, "Database persistence error during customer creation");
                return Problem(
                    title: "Database Error",
                    detail: "Failed to save customer",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error creating customer");
                //_logger.LogCritical(ex, "Critical error creating customer: {ErrorMessage}", ex.Message);
                return Problem(
                    title: "Server Error",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        // PUT: api/Customers/5
        /// <summary>
        /// Update an existing customer
        /// </summary>
        /// <param name="id">Customer identifier</param>
        /// <param name="dto">Customer data with updated values</param>
        /// <response code="204">Customer updated successfully (No content returned)</response>
        /// <response code="400">Invalid request data or validation errors</response>
        /// <response code="404">Customer with specified ID not found</response>
        /// <response code="409">Concurrency conflict or database constraint violation</response>
        /// <response code="500">Internal server error</response>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(int id, [FromBody] CustomerCreateDto dto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for customer update");
                return ValidationProblem(ModelState);
            }

            var validationResult = await _validator.ValidateAsync(dto);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed for customer update");
                validationResult.AddToModelState(ModelState);
                return ValidationProblem(ModelState);
            }

            try
            {
                var updatedCustomer = await _customerService.UpdateAsync(id, dto);

                return updatedCustomer == null
                    ? NotFound(CreateProblemDetails(
                        "Customer Not Found",
                        $"Customer with ID {id} not found",
                        StatusCodes.Status404NotFound))
                    : NoContent();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating customer {CustomerId}", id);
                return Conflict(CreateProblemDetails(
                    "Concurrency Error",
                    "Customer was modified by another user",
                    StatusCodes.Status409Conflict));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error updating customer {CustomerId}", id);
                return CreateDatabaseErrorResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating customer {CustomerId}", id);
                return CreateServerErrorResponse();
            }
        }

        // DELETE: api/Customers/5
        /// <summary>
        /// Delete an existing customer
        /// </summary>
        /// <param name="id">Unique identifier of the customer to delete</param>
        /// <response code="204">Customer successfully deleted (No content returned)</response>
        /// <response code="404">Customer with specified ID not found</response>
        /// <response code="409">Database constraint violation (e.g., related records exist)</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Attempting to delete customer {CustomerId}", id);
                var result = await _customerService.DeleteAsync(id);

                if (!result)
                {
                    _logger.LogWarning("Delete failed: Customer with ID {CustomerId} not found", id);
                    return NotFound(CreateProblemDetails(
                        title: "Resource Not Found",
                        detail: $"Customer with ID {id} does not exist",
                        statusCode: StatusCodes.Status404NotFound));
                }

                _logger.LogInformation("Customer with ID {CustomerId} successfully deleted", id);
                return NoContent();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error deleting customer ID {CustomerId}", id);

                if (ex.InnerException?.Message.Contains("FOREIGN KEY") == true)
                {
                    return Conflict(CreateProblemDetails(
                        title: "Constraint Violation",
                        detail: "Cannot delete customer with associated records",
                        statusCode: StatusCodes.Status409Conflict));
                }

                return CreateDatabaseErrorResponse();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Critical error deleting customer ID {CustomerId}", id);
                //_logger.LogError(ex, "Error deleting customer {CustomerId}", id);
                return CreateServerErrorResponse();
            }
        }

        // GET: api/Customers/rider/{riderId}
        /// <summary>
        /// Get customer by Rider ID
        /// </summary>
        /// <param name="riderId">Rider identifier</param>
        /// <response code="200">Customer found</response>
        /// <response code="404">Customer not found</response>
        /// <response code="400">Invalid rider ID format</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("rider/{riderId}")]
        [ProducesResponseType(typeof(CustomerResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CustomerResponseDto>> GetByRiderId(string riderId)
        {
            if (string.IsNullOrWhiteSpace(riderId))
            {
                _logger.LogWarning("Empty or null RiderId provided");
                return BadRequest(CreateProblemDetails(
                    "Invalid Rider ID",
                    "Rider ID cannot be empty or null",
                    StatusCodes.Status400BadRequest));
            }

            try
            {
                _logger.LogInformation("Retrieving customer with Rider ID: {RiderId}", riderId);
                var customer = await _customerService.GetByRiderIdAsync(riderId);

                return customer == null
                    ? NotFound(CreateProblemDetails("Not Found", $"Customer with Rider ID {riderId} not found", 404))
                    : Ok(customer);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving customer with Rider ID: {RiderId}", riderId);
                return Problem(
                    title: "Server Error",
                    statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        #region Helper Methods

        private bool IsUniqueConstraintViolation(DbUpdateException ex)
        {
            return ex.InnerException is SqlException sqlEx &&
                  (sqlEx.Number == 2627 || sqlEx.Number == 2601); // SQL Server error codes
        }
        private ProblemDetails CreateProblemDetails(string title, string detail, int statusCode)
        {
            return new ProblemDetails
            {
                Title = title,
                Detail = detail,
                Status = statusCode,
                Instance = HttpContext.Request.Path // Instance = Guid.NewGuid().ToString()
            };
        }

        private ObjectResult CreateDatabaseErrorResponse()
        {
            return Problem(
                title: "Database Error",
                detail: "An error occurred while accessing the database",
                statusCode: StatusCodes.Status500InternalServerError);
        }

        private ObjectResult CreateServerErrorResponse()
        {
            return Problem(
                title: "Internal Server Error",
                detail: "An unexpected error occurred",
                statusCode: StatusCodes.Status500InternalServerError);
        }
        #endregion
       
    }
}