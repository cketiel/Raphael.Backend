namespace Raphael.Api.Versioning
{
    /// <summary>
    /// The four headers that carry the conversation about versions between this API and the
    /// applications that call it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two come in and two go out. The names live here because they are a contract with
    /// Raphael.Desktop, Raphael.Driver and raphael-rider, and a contract spelled out in string
    /// literals across a dozen files is a contract that drifts (CLAUDE.md §5).
    /// </para>
    /// <para>
    /// ⚠️ The incoming pair is optional on purpose. An application that does not send them is
    /// not refused and not degraded: it is simply invisible in the compatibility answer. That
    /// matters because on the day this shipped none of the three sent anything.
    /// </para>
    /// </remarks>
    public static class ClientVersionHeaders
    {
        /// <summary>
        /// Which application is calling: <c>Desktop</c>, <c>Driver</c> or <c>Rider</c>. Sent by
        /// the client.
        /// </summary>
        public const string App = "X-Client-App";

        /// <summary>
        /// The version that application shows its user, for example <c>1.8.1</c>. Sent by the
        /// client.
        /// </summary>
        public const string Version = "X-Client-Version";

        /// <summary>
        /// <c>outdated</c> when the caller is older than the minimum this build expects.
        /// Returned by the API. Absent means nothing is known to be wrong.
        /// </summary>
        public const string Status = "X-Raphael-Client-Status";

        /// <summary>
        /// The oldest version of that application this build supports. Returned by the API
        /// alongside <see cref="Status"/>, so the client can say the number out loud instead of
        /// telling somebody to "update".
        /// </summary>
        public const string Minimum = "X-Raphael-Client-Minimum";
    }
}
