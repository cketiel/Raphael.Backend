namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// The pickup time a dispatcher settled on when moving a trip's Will Call state.
    /// </summary>
    /// <remarks>
    /// Only the time travels, because it is the only thing the dispatcher gets to decide:
    /// the flag itself, the status and the history row are the server's business.
    ///
    /// <para>
    /// ⚠️ Wall-clock time where the trip is operated, not the hour of the machine that sent
    /// it. Null means "let the server use the operation's own clock" — for an activation,
    /// now where the trip runs; for the reverse, the 23:59 the office reads as waiting on
    /// the patient. See <c>_meta/TIME_POLICY.md</c> §2B.
    /// </para>
    /// </remarks>
    public class WillCallUpdateDto
    {
        public TimeSpan? FromTime { get; set; }
    }
}
