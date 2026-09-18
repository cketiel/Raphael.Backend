using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Raphael.Api.Versioning;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;

namespace Raphael.Api.Controllers
{
    /// <summary>
    /// What is deployed here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For a year the only thing that identified this API was its Swagger page, and Swagger does
    /// not say which build it is — <c>v1</c> there is the name of the document, not of the
    /// binary. So the answer to "which version is in production?" was somebody's memory of the
    /// last time they uploaded it. That is not an answer you can put on a ticket, and it is not
    /// an answer at all once Swagger is off in production.
    /// </para>
    /// <para>
    /// Two endpoints, on purpose:
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <c>GET /api/version</c> is anonymous and says two things. An application has to be
    ///     able to ask before anybody has signed in — that is the whole point of a compatibility
    ///     check — and a person handling a ticket should not need credentials to say which build
    ///     answered.
    ///   </item>
    ///   <item>
    ///     <c>GET /api/version/details</c> needs a token and says everything: commit, build
    ///     time, schema, the client versions this build expects.
    ///   </item>
    /// </list>
    /// </remarks>
    [ApiController]
    [Route("api/version")]
    public class VersionController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IOptionsMonitor<ClientCompatibilityOptions> _compatibility;
        private readonly RaphaelContext _context;
        private readonly ILogger<VersionController> _logger;

        public VersionController(
            IWebHostEnvironment environment,
            IOptionsMonitor<ClientCompatibilityOptions> compatibility,
            RaphaelContext context,
            ILogger<VersionController> logger)
        {
            _environment = environment;
            _compatibility = compatibility;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// The build serving this request, and which environment it is.
        /// </summary>
        /// <remarks>
        /// Anonymous. It publishes the version of our own code, which is a fingerprint, and that
        /// was weighed: the cost is that an attacker learns which build is running, and the
        /// benefit is that every client can check it is current before login and every ticket
        /// can name the build without credentials. Nothing here describes the inside of the
        /// system, and nothing here is patient data.
        /// </remarks>
        /// <response code="200">Always.</response>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiVersionDto), StatusCodes.Status200OK)]
        public ActionResult<ApiVersionDto> Get() => Ok(new ApiVersionDto
        {
            Version = BuildInfo.Version,
            Environment = _environment.EnvironmentName
        });

        /// <summary>
        /// Everything about this deployment: commit, build time, schema and expected client
        /// versions.
        /// </summary>
        /// <response code="200">Always, for an authenticated caller.</response>
        /// <response code="401">No token, or an expired one.</response>
        [HttpGet("details")]
        [ProducesResponseType(typeof(ApiVersionDetailsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<ApiVersionDetailsDto>> GetDetails(
            CancellationToken cancellationToken)
        {
            return Ok(new ApiVersionDetailsDto
            {
                Version = BuildInfo.Version,
                Commit = BuildInfo.Commit,
                BuiltUtc = BuildInfo.BuiltUtc,
                Environment = _environment.EnvironmentName,
                ApiContract = ApiContract.Document,
                IntegrationContract = ApiContract.IntegrationSpec,
                DatabaseMigration = await LastMigrationAsync(cancellationToken),
                MinimumClientVersions =
                    new Dictionary<string, string>(_compatibility.CurrentValue.MinimumVersions)
            });
        }

        /// <summary>
        /// The last migration the database reports as applied, or <see langword="null"/> if it
        /// cannot be asked.
        /// </summary>
        /// <remarks>
        /// ⚠️ Swallows the failure deliberately, which is the opposite of what startup does with
        /// the same operation. This endpoint earns its keep during an incident, and an incident
        /// is exactly when the database may be the thing that is broken. Returning the build
        /// number with an unknown schema is useful; returning a 500 because the schema could not
        /// be read tells the person nothing at the moment they need something.
        /// </remarks>
        private async Task<string?> LastMigrationAsync(CancellationToken cancellationToken)
        {
            try
            {
                var applied = await _context.Database
                    .GetAppliedMigrationsAsync(cancellationToken);

                return applied.LastOrDefault();
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Could not read the applied migrations while answering /api/version/details.");

                return null;
            }
        }
    }
}
