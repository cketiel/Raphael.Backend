using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Auth;
using Raphael.Api.Services.CallRequests;
using Raphael.Shared.DTOs.CallRequests;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// The dispatch office working through drivers' requests to be called back.
    /// </summary>
    /// <remarks>
    /// Office staff only, decided by <see cref="ICallerRoles"/> the same way the notification hub
    /// decides it. A driver, a patient or an integration gets 403 on every endpoint here.
    /// </remarks>
    [Authorize]
    [ApiController]
    [Route("api/call-requests")]
    public class CallRequestsController : ControllerBase
    {
        private readonly ICallRequestService _callRequests;
        private readonly ICallerRoles _roles;

        public CallRequestsController(ICallRequestService callRequests, ICallerRoles roles)
        {
            _callRequests = callRequests;
            _roles = roles;
        }

        [HttpGet]
        public async Task<IActionResult> GetQueue(CancellationToken cancellationToken)
        {
            var caller = await CallerAsync(cancellationToken);

            if (caller is null)
                return Forbid();

            return Ok(await _callRequests.GetQueueAsync(caller, cancellationToken));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
        {
            var caller = await CallerAsync(cancellationToken);

            if (caller is null)
                return Forbid();

            var detail = await _callRequests.GetDetailAsync(caller, id, cancellationToken);

            return detail is null ? NotFound() : Ok(detail);
        }

        [HttpPost("{id:int}/claim")]
        public Task<IActionResult> Claim(int id, CancellationToken cancellationToken) =>
            ChangeAsync(caller => _callRequests.ClaimAsync(caller, id, cancellationToken), cancellationToken);

        [HttpPost("{id:int}/take-over")]
        public Task<IActionResult> TakeOver(int id, CancellationToken cancellationToken) =>
            ChangeAsync(caller => _callRequests.TakeOverAsync(caller, id, cancellationToken), cancellationToken);

        [HttpPost("{id:int}/release")]
        public Task<IActionResult> Release(int id, CancellationToken cancellationToken) =>
            ChangeAsync(caller => _callRequests.ReleaseAsync(caller, id, cancellationToken), cancellationToken);

        [HttpPost("{id:int}/no-answer")]
        public Task<IActionResult> NoAnswer(int id, CancellationToken cancellationToken) =>
            ChangeAsync(caller => _callRequests.NoAnswerAsync(caller, id, cancellationToken), cancellationToken);

        [HttpPost("{id:int}/resolve")]
        public Task<IActionResult> Resolve(
            int id,
            [FromBody] ResolveCallRequestDto input,
            CancellationToken cancellationToken) =>
            ChangeAsync(caller => _callRequests.ResolveAsync(caller, id, input, cancellationToken), cancellationToken);

        [HttpPost("{id:int}/reopen")]
        public Task<IActionResult> Reopen(int id, CancellationToken cancellationToken) =>
            ChangeAsync(caller => _callRequests.ReopenAsync(caller, id, cancellationToken), cancellationToken);

        private async Task<IActionResult> ChangeAsync(
            Func<OfficeCaller, Task<CallRequestResult<CallRequestSummaryDto>>> change,
            CancellationToken cancellationToken)
        {
            var caller = await CallerAsync(cancellationToken);

            if (caller is null)
                return Forbid();

            var result = await change(caller);

            return result.Outcome switch
            {
                CallRequestOutcome.Ok => Ok(result.Value),
                CallRequestOutcome.NotFound => NotFound(),
                CallRequestOutcome.Conflict => Conflict(new CallRequestConflictDto
                {
                    Message = result.Message ?? string.Empty,
                    Current = result.Value
                }),
                _ => BadRequest(new CallRequestConflictDto { Message = result.Message ?? string.Empty })
            };
        }

        private async Task<OfficeCaller?> CallerAsync(CancellationToken cancellationToken)
        {
            if (!_roles.IsOffice(User))
                return null;

            return await _callRequests.ResolveOfficeCallerAsync(
                _roles.UserIdOf(User)!.Value,
                _roles.ProviderIdOf(User),
                cancellationToken);
        }
    }
}
