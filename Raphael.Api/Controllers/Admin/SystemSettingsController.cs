using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Raphael.Api.Services.Admin;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities.Routing;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Controllers.Admin
{
    /// <summary>
    /// The settings an administrator can change while the system is running.
    /// </summary>
    /// <remarks>
    /// ⚠️ Administrators only. The first setting here decides how much every travel time in the
    /// ecosystem costs, so the role is checked the same way the notification admin panel checks
    /// it: against <c>ClaimTypes.Role</c>, which login issues as the numeric <c>RoleId</c>, and
    /// role 1 is Administrator.
    /// </remarks>
    [ApiController]
    [Route("api/admin/settings")]
    [Authorize(Roles = "1")]
    public sealed class SystemSettingsController : ControllerBase
    {
        private readonly ISystemSettingService _settings;
        private readonly ICurrentUserService _currentUser;

        public SystemSettingsController(
            ISystemSettingService settings,
            ICurrentUserService currentUser)
        {
            _settings = settings;
            _currentUser = currentUser;
        }

        /// <summary>Everything that has ever been set.</summary>
        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SystemSettingDto>>> GetAll(
            CancellationToken cancellationToken)
        {
            return Ok(await _settings.GetAllAsync(cancellationToken));
        }

        /// <summary>One setting by key.</summary>
        [HttpGet("{key}")]
        public async Task<ActionResult<SystemSettingDto>> Get(
            string key,
            CancellationToken cancellationToken)
        {
            var setting = await _settings.GetOneAsync(key, cancellationToken);

            return setting is null ? NotFound() : Ok(setting);
        }

        /// <summary>
        /// Changes a setting. Live within a minute, everywhere, with no deployment.
        /// </summary>
        [HttpPut("{key}")]
        public async Task<ActionResult<SystemSettingDto>> Set(
            string key,
            [FromBody] SystemSettingUpdateDto request,
            CancellationToken cancellationToken)
        {
            if (request is null || string.IsNullOrWhiteSpace(request.Value))
            {
                return BadRequest("A value is required.");
            }

            // Validated here rather than trusted: an unrecognised traffic mode would fall back to
            // MaxSavings silently, and an administrator who typed "precision " with a trailing
            // space would spend a week believing they had bought accuracy.
            if (key == SystemSettingKeys.RoutingTrafficMode
                && !Enum.TryParse<RoutingTrafficMode>(request.Value, ignoreCase: true, out _))
            {
                return BadRequest(
                    $"'{request.Value}' is not a traffic mode. Use " +
                    $"'{nameof(RoutingTrafficMode.MaxSavings)}' or " +
                    $"'{nameof(RoutingTrafficMode.Precision)}'.");
            }

            if (key == SystemSettingKeys.RoutingDefaultBufferPercent
                && (!int.TryParse(request.Value, out var percent) || percent is < 0 or > 100))
            {
                return BadRequest("The buffer must be a whole percentage between 0 and 100.");
            }

            var saved = await _settings.SetAsync(
                key,
                request.Value.Trim(),
                _currentUser.UserName,
                cancellationToken);

            return Ok(saved);
        }
    }
}
