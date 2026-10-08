using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.BookingAdmin;
using Raphael.Shared.DTOs.BookingAdmin;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The Booking Portal's Admin tab: a clinic's users, its own record and API key, its funding
    /// source, and the billing items and rates it keeps on it (BOOKING_ADMIN.md).
    /// </summary>
    /// <remarks>
    /// Only a clinic's admin (role 1 with an integrator). The office has role 1 too but no
    /// integrator; the service refuses it with a 403, because the office manages all of this from
    /// the Desktop. Every call is scoped to the caller's integrator, read from the token.
    /// </remarks>
    [ApiController]
    [Route("api/BookingPortal/admin")]
    [Authorize(Roles = "1")]
    public class BookingPortalAdminController : ControllerBase
    {
        private readonly IBookingAdminService _admin;

        public BookingPortalAdminController(IBookingAdminService admin)
        {
            _admin = admin;
        }

        // ------------------------------------------------------------------ users

        [HttpGet("roles")]
        [ProducesResponseType(typeof(List<BookingAdminRoleDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<BookingAdminRoleDto>>> GetRoles(CancellationToken ct) =>
            Run(() => _admin.GetRolesAsync(ct));

        [HttpGet("users")]
        [ProducesResponseType(typeof(List<BookingAdminUserDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<BookingAdminUserDto>>> GetUsers(CancellationToken ct) =>
            Run(() => _admin.GetUsersAsync(ct));

        [HttpPost("users")]
        [ProducesResponseType(typeof(BookingAdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<BookingAdminUserDto>> CreateUser([FromBody] BookingAdminUserCreateDto dto, CancellationToken ct) =>
            Run(() => _admin.CreateUserAsync(dto, ct));

        [HttpPut("users/{id:int}")]
        [ProducesResponseType(typeof(BookingAdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<BookingAdminUserDto>> EditUser(int id, [FromBody] BookingAdminUserEditDto dto, CancellationToken ct) =>
            Run(() => _admin.EditUserAsync(id, dto, ct));

        /// <remarks>Ends every open session of that user.</remarks>
        [HttpPut("users/{id:int}/password")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<bool>> SetPassword(int id, [FromBody] BookingAdminPasswordDto dto, CancellationToken ct) =>
            Run(async () => { await _admin.SetPasswordAsync(id, dto, ct); return true; }, noContent: true);

        /// <remarks>Disabling ends every open session of that user.</remarks>
        [HttpPut("users/{id:int}/active")]
        [ProducesResponseType(typeof(BookingAdminUserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<BookingAdminUserDto>> SetActive(int id, [FromBody] BookingAdminUserActiveDto dto, CancellationToken ct) =>
            Run(() => _admin.SetActiveAsync(id, dto.IsActive, ct));

        // ------------------------------------------------------------------ organization

        [HttpGet("organization")]
        [ProducesResponseType(typeof(BookingAdminOrganizationDto), StatusCodes.Status200OK)]
        public Task<ActionResult<BookingAdminOrganizationDto>> GetOrganization(CancellationToken ct) =>
            Run(() => _admin.GetOrganizationAsync(ct));

        /// <remarks>A name and zip that belong to another catalog entry answer 409 (global handler).</remarks>
        [HttpPut("organization")]
        [ProducesResponseType(typeof(BookingAdminOrganizationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public Task<ActionResult<BookingAdminOrganizationDto>> EditOrganization([FromBody] BookingAdminOrganizationEditDto dto, CancellationToken ct) =>
            Run(() => _admin.EditOrganizationAsync(dto, ct));

        /// <remarks>Apart from the record, so the key only travels when somebody asks to see it.</remarks>
        [HttpGet("organization/api-key")]
        [ProducesResponseType(typeof(BookingAdminApiKeyDto), StatusCodes.Status200OK)]
        public Task<ActionResult<BookingAdminApiKeyDto>> GetApiKey(CancellationToken ct) =>
            Run(() => _admin.GetApiKeyAsync(ct));

        // ------------------------------------------------------------------ funding source

        /// <remarks>204 when the clinic has none yet: the screen then offers to create it.</remarks>
        [HttpGet("funding-source")]
        [ProducesResponseType(typeof(BookingAdminFundingSourceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<ActionResult<BookingAdminFundingSourceDto>> GetFundingSource(CancellationToken ct)
        {
            try
            {
                var fundingSource = await _admin.GetFundingSourceAsync(ct);
                return fundingSource is null ? NoContent() : Ok(fundingSource);
            }
            catch (BookingAdminDeniedException denied)
            {
                return denied.NotFound ? NotFound() : Forbid();
            }
        }

        [HttpPost("funding-source")]
        [ProducesResponseType(typeof(BookingAdminFundingSourceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<BookingAdminFundingSourceDto>> CreateFundingSource([FromBody] BookingAdminFundingSourceEditDto dto, CancellationToken ct) =>
            Run(() => _admin.CreateFundingSourceAsync(dto, ct));

        [HttpPut("funding-source")]
        [ProducesResponseType(typeof(BookingAdminFundingSourceDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<BookingAdminFundingSourceDto>> EditFundingSource([FromBody] BookingAdminFundingSourceEditDto dto, CancellationToken ct) =>
            Run(() => _admin.EditFundingSourceAsync(dto, ct));

        // ------------------------------------------------------------------ billing items

        [HttpGet("billing-items")]
        [ProducesResponseType(typeof(List<BookingAdminBillingItemDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<BookingAdminBillingItemDto>>> GetBillingItems(CancellationToken ct) =>
            Run(() => _admin.GetBillingItemsAsync(ct));

        [HttpPost("billing-items")]
        [ProducesResponseType(typeof(BookingAdminBillingItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<BookingAdminBillingItemDto>> CreateBillingItem([FromBody] BookingAdminBillingItemEditDto dto, CancellationToken ct) =>
            Run(() => _admin.CreateBillingItemAsync(dto, ct));

        [HttpPut("billing-items/{id:int}")]
        [ProducesResponseType(typeof(BookingAdminBillingItemDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<BookingAdminBillingItemDto>> EditBillingItem(int id, [FromBody] BookingAdminBillingItemEditDto dto, CancellationToken ct) =>
            Run(() => _admin.EditBillingItemAsync(id, dto, ct));

        /// <remarks>Only an item that was never rated: one with rates has them closed instead.</remarks>
        [HttpDelete("billing-items/{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<bool>> DeleteBillingItem(int id, CancellationToken ct) =>
            Run(async () => { await _admin.DeleteBillingItemAsync(id, ct); return true; }, noContent: true);

        // ------------------------------------------------------------------ rates

        [HttpGet("rates")]
        [ProducesResponseType(typeof(List<BookingAdminRateDto>), StatusCodes.Status200OK)]
        public Task<ActionResult<List<BookingAdminRateDto>>> GetRates(CancellationToken ct) =>
            Run(() => _admin.GetRatesAsync(ct));

        [HttpPost("rates")]
        [ProducesResponseType(typeof(BookingAdminRateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public Task<ActionResult<BookingAdminRateDto>> CreateRate([FromBody] BookingAdminRateEditDto dto, CancellationToken ct) =>
            Run(() => _admin.CreateRateAsync(dto, ct));

        /// <remarks>Rates are never deleted: one that no longer applies gets its end date.</remarks>
        [HttpPut("rates/{id:int}")]
        [ProducesResponseType(typeof(BookingAdminRateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public Task<ActionResult<BookingAdminRateDto>> EditRate(int id, [FromBody] BookingAdminRateEditDto dto, CancellationToken ct) =>
            Run(() => _admin.EditRateAsync(id, dto, ct));

        /// <summary>Turns the service's refusals into 403, 404 and 400; anything else reaches the global handler.</summary>
        private async Task<ActionResult<T>> Run<T>(Func<Task<T>> work, bool noContent = false)
        {
            try
            {
                var result = await work();
                return noContent ? NoContent() : Ok(result);
            }
            catch (BookingAdminDeniedException denied)
            {
                return denied.NotFound ? NotFound() : Forbid();
            }
            catch (BookingAdminRuleException rule)
            {
                return BadRequest(rule.Message);
            }
        }
    }
}
