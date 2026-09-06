using Raphael.Shared.DTOs;
using System;

namespace Raphael.Api.Services.Import
{
    /// <summary>
    /// A CSV row refused because the journey it describes is already booked under another
    /// identifier, carrying the trip it collided with.
    /// </summary>
    /// <remarks>
    /// Deliberately NOT an <c>IntegrationRejectedException</c> with an extra field on it. Those
    /// types are the integrator contract - INTEGRATION_API_SPEC.md 6.1 - and the import was
    /// written to reuse them untouched. Adding a payload there would put the details of somebody
    /// else's booking one careless line away from the integrator responses, which must never
    /// name a record the integrator does not own.
    ///
    /// This one exists only on the import path, is caught only by ImportTripsAsync, and reaches
    /// only a dispatcher holding a JWT.
    /// </remarks>
    public sealed class TripImportConflictException : Exception
    {
        public TripImportConflictException(string message, TripImportConflictDto conflict)
            : base(message)
        {
            Conflict = conflict;
        }

        public TripImportConflictDto Conflict { get; }
    }
}
