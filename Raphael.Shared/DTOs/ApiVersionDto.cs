namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// The least a caller needs in order to know what it is talking to: which build, and whether
    /// it is production.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Deliberately two fields. This is the one version answer served without a token, so it
    /// carries what an application needs before anybody has signed in — to check it is current,
    /// and to refuse to show production data on a screen that was pointed at development by
    /// mistake — and nothing that describes the inside of the system. The commit, the build
    /// time and the state of the database live in <see cref="ApiVersionDetailsDto"/>, behind
    /// authentication.
    /// </para>
    /// </remarks>
    public class ApiVersionDto
    {
        /// <summary>
        /// The build serving this request, for example <c>1.4.0</c>. It is the git tag: the
        /// deployment refuses to publish a tag that disagrees with it.
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Which environment this is: <c>Production</c>, <c>Development</c>. What
        /// <c>ASPNETCORE_ENVIRONMENT</c> says.
        /// </summary>
        public string Environment { get; set; } = string.Empty;
    }
}
