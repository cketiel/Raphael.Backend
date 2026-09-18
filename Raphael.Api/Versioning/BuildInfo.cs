using System.Globalization;
using System.Reflection;

namespace Raphael.Api.Versioning
{
    /// <summary>
    /// What this build is: the version it declares, the commit it came from and when it was
    /// produced.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Read once from the assembly. None of it changes while the process runs, and the question
    /// it answers — "which build is serving this?" — is usually asked in the middle of an
    /// incident, which is the worst moment for it to cost anything.
    /// </para>
    /// <para>
    /// The version is declared in <c>Directory.Build.props</c>, and the deployment workflow
    /// refuses to publish a tag whose name disagrees with it. That is what makes this number
    /// trustworthy on a support ticket: <c>1.4.0</c> here can only have come from the commit
    /// tagged <c>v1.4.0</c>.
    /// </para>
    /// <para>
    /// ⚠️ <see cref="Commit"/> and <see cref="BuiltUtc"/> are injected by the build
    /// (<c>SourceRevisionId</c> and <c>BuildTimestampUtc</c>), which only CI passes. They are
    /// therefore empty in a build somebody made on their own machine, and that absence is the
    /// signal, not a defect: a deployed build always has both.
    /// </para>
    /// </remarks>
    public static class BuildInfo
    {
        /// <summary>Key of the assembly metadata entry the build writes the timestamp into.</summary>
        private const string BuildTimestampKey = "BuildTimestampUtc";

        static BuildInfo()
        {
            var assembly = typeof(BuildInfo).Assembly;

            // "1.4.0+9f3c1a…" once the build has been given a revision, "1.4.0" otherwise.
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "0.0.0";

            var plus = informational.IndexOf('+');

            Version = plus < 0 ? informational : informational[..plus];

            Commit = plus < 0 || plus == informational.Length - 1
                ? null
                : informational[(plus + 1)..];

            var stamp = assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(attribute => attribute.Key == BuildTimestampKey)?
                .Value;

            // Round-trip format, so the value carries its own offset and is read back as the
            // instant it was written -- never as wall-clock time on whichever machine parses it.
            BuiltUtc = DateTime.TryParse(
                stamp,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind | DateTimeStyles.AdjustToUniversal,
                out var parsed)
                ? DateTime.SpecifyKind(parsed, DateTimeKind.Utc)
                : null;
        }

        /// <summary>The declared version, without build metadata. For example <c>1.4.0</c>.</summary>
        public static string Version { get; }

        /// <summary>
        /// The full commit hash this was built from, or <see langword="null"/> in a build that
        /// did not come from CI.
        /// </summary>
        public static string? Commit { get; }

        /// <summary>
        /// When CI produced this build, or <see langword="null"/> in a build that did not come
        /// from CI. Always an instant in UTC.
        /// </summary>
        public static DateTime? BuiltUtc { get; }
    }
}
