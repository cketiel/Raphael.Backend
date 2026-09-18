namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// Everything needed to answer a support ticket about a deployed API: which build, from
    /// which commit, produced when, against which database schema, and which client versions it
    /// expects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Behind authentication, for the same reason <c>/health/ready</c> is: the state of the
    /// database and the commit a binary was cut from are facts about the inside of the system,
    /// and there is no reason to publish them. The number by itself is public — see
    /// <see cref="ApiVersionDto"/>.
    /// </para>
    /// <para>
    /// Carries no patient data and never will. It is the one page a support engineer is
    /// guaranteed to open during an incident, which makes it the worst possible place to put
    /// something that must not be pasted into a ticket (CLAUDE.md §3).
    /// </para>
    /// </remarks>
    public class ApiVersionDetailsDto
    {
        /// <summary>The build serving this request, for example <c>1.4.0</c>.</summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Full commit hash this was built from. <see langword="null"/> in a build that did not
        /// come from CI, which is itself the answer: it was not deployed by the workflow.
        /// </summary>
        public string? Commit { get; set; }

        /// <summary>
        /// When CI produced this build, in UTC. <see langword="null"/> for the same reason as
        /// <see cref="Commit"/>.
        /// </summary>
        public DateTime? BuiltUtc { get; set; }

        /// <summary>Which environment this is. What <c>ASPNETCORE_ENVIRONMENT</c> says.</summary>
        public string Environment { get; set; } = string.Empty;

        /// <summary>
        /// Version of the OpenAPI document, <c>v1</c>. Moves only on a breaking change to the
        /// shape of the API, and so is almost never the answer to "why did this stop working".
        /// </summary>
        public string ApiContract { get; set; } = string.Empty;

        /// <summary>
        /// Version of <c>INTEGRATION_API_SPEC.md</c> this build implements. This is the number
        /// an external integrator codes against.
        /// </summary>
        public string IntegrationContract { get; set; } = string.Empty;

        /// <summary>
        /// Name of the last migration applied to the database this API is connected to, for
        /// example <c>20260910_AddEarlyArrivalWait</c>.
        /// </summary>
        /// <remarks>
        /// The API applies its migrations at startup and refuses to start if it cannot, so in a
        /// healthy deployment this is the last migration the build carries. It is worth
        /// reporting anyway: it is what says whether rolling back to the previous tag is even
        /// possible, because redeploying older code does not undo a schema change.
        /// </remarks>
        public string? DatabaseMigration { get; set; }

        /// <summary>
        /// Oldest version of each client application this build expects to serve, keyed by the
        /// name the application sends in <c>X-Client-App</c>. Advice, not a gate: an older
        /// client is marked in the response and served normally.
        /// </summary>
        public Dictionary<string, string> MinimumClientVersions { get; set; } = new();
    }
}
