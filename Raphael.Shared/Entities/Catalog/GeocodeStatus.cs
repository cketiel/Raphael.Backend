namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>
    /// Whether a catalog row has coordinates, and why it does not when it does not.
    /// </summary>
    /// <remarks>
    /// Geocoding is on demand. The source files bring no coordinates, and geocoding the whole
    /// catalog at once is not possible anyway: the Geocoding API allows 1,500 calls a day and the
    /// catalog is thousands of rows. So a row arrives as <see cref="NotRequested"/> and only moves
    /// when someone asks for it — see MAPS_POLICY.md.
    /// </remarks>
    public enum GeocodeStatus
    {
        /// <summary>Nobody has asked. The normal state of an imported row.</summary>
        NotRequested = 0,

        /// <summary>Coordinates came in the source file; we did not pay for them.</summary>
        FromSource = 1,

        /// <summary>Resolved through the routing proxy.</summary>
        Resolved = 2,

        /// <summary>Google answered, and the answer was that it does not know this address.</summary>
        NotFound = 3,

        /// <summary>The call failed. Distinct from <see cref="NotFound"/>: this one is worth retrying.</summary>
        Failed = 4
    }
}
