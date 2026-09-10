namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// The handful of running settings a dispatch screen needs in order to draw itself.
    /// </summary>
    /// <remarks>
    /// ⚠️ Deliberately not the administration endpoint. <c>SystemSettingsController</c> is role 1
    /// only — it holds settings that decide what the office spends — and a dispatcher must not be
    /// let into it just to find out when to colour a row. This carries the one number the Schedule
    /// tab needs and nothing else, so widening it later is a decision somebody has to take on
    /// purpose.
    ///
    /// <para>
    /// Read-only. Changing a setting stays where it was, behind the administrator's panel.
    /// </para>
    /// </remarks>
    public class DispatchSettingsDto
    {
        /// <summary>
        /// Minutes of driver waiting at a pickup from which the whole row is marked. The chip on
        /// the arrival hour shows any wait at all; this is only where it becomes worth a colour.
        /// </summary>
        public int EarlyArrivalWaitHighlightMinutes { get; set; }
    }
}
