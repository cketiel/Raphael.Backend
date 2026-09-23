namespace Raphael.Api.Services.CallRequests
{
    public sealed class CallRequestOptions
    {
        public const string SectionName = "CallRequests";

        /// <summary>
        /// Minimum gap between two presses of the driver's button that count. Without it the
        /// reminder counter measures how hard someone taps, not how long they have waited.
        /// </summary>
        public int MinSecondsBetweenDriverSignals { get; set; } = 120;
    }
}
