namespace Raphael.Api.Services.Integration
{
    /// <summary>
    /// A trip refused on its own merits, before the database was ever asked.
    /// </summary>
    /// <remarks>
    /// Carries the answer already worded for the integrator. These are expected outcomes
    /// rather than faults: the conditions are checked for deliberately so the integrator
    /// is told what is wrong with the trip they sent, instead of being handed whatever
    /// SQL Server would have said once the insert had already failed.
    ///
    /// <para>
    /// The exception's own <see cref="Exception.Message"/> is only the code. The wording
    /// meant for the integrator can name their trip, so it stays in <see cref="Error"/>
    /// and travels the response, not the log.
    /// </para>
    /// </remarks>
    public sealed class IntegrationRejectedException : Exception
    {
        public IntegrationRejectedException(IntegrationSyncError error)
            : base($"Trip rejected: {error.Code}")
        {
            Error = error;
        }

        /// <summary>What the integrator is told, and whether retrying could help.</summary>
        public IntegrationSyncError Error { get; }
    }
}
