namespace Raphael.Shared.Entities
{
    /// <summary>
    /// What a Provider that works outside Raphael reported about a trip given to it
    /// (PROVIDER_ACCESS_MODEL.md §3.5). Not a <see cref="TripStatus"/>: that one is shared with the
    /// Desktop, the Driver, the Rider and the integration API, and a rejection does not move it.
    /// </summary>
    public enum ProviderOutcome
    {
        /// <summary>The Provider will not do it. The trip keeps the Provider, marked, until the clinic decides.</summary>
        Rejected = 1,

        /// <summary>Done. The trip is Finished and carries the real times, which the production report reads.</summary>
        Completed = 2,

        /// <summary>The patient was not there. Cancelled as a driver's no-show, the return with it.</summary>
        NoShow = 3,
    }
}
