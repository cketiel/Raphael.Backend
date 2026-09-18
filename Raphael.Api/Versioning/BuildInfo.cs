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

        //
        // ⚠️ Nothing in here may throw, and that is not defensive habit -- it is the lesson from
        // 2026-09-18, when it did.
        //
        // The first version of this combined two DateTimeStyles that cannot be combined, which
        // threw ArgumentException from the static constructor. A static constructor that throws
        // poisons the type for the lifetime of the process: every later read of any member
        // raises TypeInitializationException. So GET /api/version returned 500 -- an endpoint
        // that does not read the timestamp at all, failing because of the timestamp.
        //
        // This type answers "which build is this?" during an incident. It is the last thing that
        // may be the reason an incident cannot be diagnosed, so a field that cannot be read is
        // reported as absent and never as a failure.
        //
        static BuildInfo()
        {
            var version = "0.0.0";
            string? commit = null;
            DateTime? builtUtc = null;

            try
            {
                var assembly = typeof(BuildInfo).Assembly;

                // "1.4.0+9f3c1a…" once the build has been given a revision, "1.4.0" otherwise.
                var informational = assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                    .InformationalVersion ?? version;

                var plus = informational.IndexOf('+');

                version = plus < 0 ? informational : informational[..plus];

                commit = plus < 0 || plus == informational.Length - 1
                    ? null
                    : informational[(plus + 1)..];

                var stamp = assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(attribute => attribute.Key == BuildTimestampKey)?
                    .Value;

                // Read as a DateTimeOffset, so the offset written into the string is what decides
                // the instant. A DateTime would have to be told how to treat a value with no
                // offset, and the only answer available to it is the host's timezone -- which is
                // never a valid input here (CLAUDE.md §5.1). UtcDateTime then converts by
                // arithmetic on that offset and consults no clock and no zone.
                if (DateTimeOffset.TryParse(
                        stamp,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var parsed))
                {
                    builtUtc = parsed.UtcDateTime;
                }
            }
            catch
            {
                // Whatever could not be read stays at its default and is reported as absent.
            }

            Version = version;
            Commit = commit;
            BuiltUtc = builtUtc;
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
