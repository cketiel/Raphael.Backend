namespace Raphael.Api.Versioning
{
    /// <summary>
    /// The oldest version of each client application this build of the API expects to serve.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Configuration and not a constant, because the answer changes with a deployment of the
    /// API and not with a rebuild of it: raising the floor for Raphael.Desktop is an operational
    /// decision taken the day its release is actually distributed, which is never the day the
    /// server code was written.
    /// </para>
    /// <para>
    /// ⚠️ This is advice, never a gate. <see cref="ClientCompatibilityMiddleware"/> marks an old
    /// caller and serves it exactly as it would a current one. Refusing a driver mid-route
    /// because the phone in his pocket is one release behind is a worse outcome than whatever
    /// the old version gets wrong.
    /// </para>
    /// </remarks>
    public sealed class ClientCompatibilityOptions
    {
        /// <summary>Configuration section this is bound from.</summary>
        public const string SectionName = "ClientCompatibility";

        /// <summary>
        /// Minimum version by application name, keyed by the value the client sends in
        /// <see cref="ClientVersionHeaders.App"/>. An application missing from here is never
        /// reported as outdated.
        /// </summary>
        public Dictionary<string, string> MinimumVersions { get; set; } =
            new(StringComparer.OrdinalIgnoreCase);
    }
}
