namespace Raphael.Shared.Entities
{
    /// <summary>
    /// How a Provider carries out the trips given to it (PROVIDER_ACCESS_MODEL.md §3.2).
    /// </summary>
    /// <remarks>
    /// One account model for both: trips, history and clinic relations hang off the same
    /// <see cref="Provider.Id"/> in either mode, so moving a Provider from an outside system to
    /// Raphael is changing this value and giving it Desktop users. Nothing is migrated.
    /// </remarks>
    public enum ProviderWorkMode
    {
        /// <summary>Works its trips in Raphael.Desktop. Every account made before 2026-10-11.</summary>
        Raphael = 0,

        /// <summary>
        /// Works its trips in another system (Mediroute…). Its users only enter the Booking portal,
        /// to see and export the trips the clinics give it, and are never treated as office.
        /// </summary>
        External = 1,
    }
}
