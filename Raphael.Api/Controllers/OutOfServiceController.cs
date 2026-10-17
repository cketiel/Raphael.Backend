using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.OutOfService;
using Raphael.Shared.Definitions.OutOfService;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// Vehicles and drivers out of service, and so their routes (RE-053). Desktop only: no
    /// [AllowApps], so the app filter keeps every other application out. JWT, never an API Key.
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/out-of-service")]
    public class OutOfServiceController : ControllerBase
    {
        private readonly IOutOfServiceService _service;

        public OutOfServiceController(IOutOfServiceService service)
        {
            _service = service;
        }

        /// <summary>Every vehicle and driver out of service now.</summary>
        [HttpGet("active")]
        public async Task<ActionResult<List<OutOfServiceRecordDto>>> GetActive(CancellationToken ct) =>
            Ok(await _service.GetActiveAsync(ct));

        /// <summary>The reasons the dialog suggests.</summary>
        [HttpGet("reasons")]
        public ActionResult<OutOfServiceReasonCatalogDto> GetReasons() => Ok(new OutOfServiceReasonCatalogDto
        {
            Vehicle = OutOfServiceReasonCodes.Vehicle.ToList(),
            Driver = OutOfServiceReasonCodes.Driver.ToList(),
            NoteMaxLength = OutOfServiceReasonCodes.NoteMaxLength
        });

        /// <summary>What taking these out of service would do. Nothing is saved.</summary>
        [HttpPost("preview")]
        public async Task<ActionResult<OutOfServicePreviewDto>> Preview([FromBody] OutOfServiceRequestDto request, CancellationToken ct)
        {
            if (request == null) return BadRequest();

            try
            {
                return Ok(await _service.PreviewAsync(request, ct));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Takes them out of service, in one transaction, with the preview's fingerprint. Answers
        /// with what was done.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<OutOfServicePreviewDto>> Execute([FromBody] OutOfServiceRequestDto request, CancellationToken ct)
        {
            if (request == null) return BadRequest();

            try
            {
                // The request's own cancellation is not passed: a dispatcher closing the dialog
                // mid-way must not leave a transaction that the client thinks never ran.
                return Ok(await _service.ExecuteAsync(request, CancellationToken.None));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>Puts one vehicle or driver back in service. Its trips do not come back.</summary>
        [HttpPost("{id:int}/return")]
        public async Task<ActionResult<OutOfServiceRecordDto>> ReturnToService(int id, CancellationToken ct)
        {
            try
            {
                return Ok(await _service.ReturnToServiceAsync(id, ct));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}
