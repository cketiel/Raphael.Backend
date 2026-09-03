using System;
using System.Collections.Generic;

namespace Raphael.Shared.DTOs
{
    /// <summary>
    /// One stop's place in the route, and what the router worked out for it.
    /// </summary>
    public class ScheduleStopSequenceDto
    {
        public int Id { get; set; }

        public int? Sequence { get; set; }

        public TimeSpan? ETA { get; set; }

        public TimeSpan? Travel { get; set; }

        public double? Distance { get; set; }
    }

    /// <summary>
    /// A whole route's new order, sent in one piece.
    /// </summary>
    /// <remarks>
    /// Dragging one stop renumbers every stop after it, and each of those used to be its own
    /// PUT — against a database that is on the internet, so the dispatcher waited for a round
    /// trip per stop while the grid sat still. This carries the lot in one request and one
    /// transaction, so the route is never half-renumbered.
    ///
    /// The route and the date travel with it and are not decoration: they are what the server
    /// checks the stops against, so a request cannot renumber stops belonging to another route
    /// or another day.
    /// </remarks>
    public class ScheduleResequenceRequest
    {
        public int VehicleRouteId { get; set; }

        public DateTime Date { get; set; }

        public List<ScheduleStopSequenceDto> Stops { get; set; } = new();
    }
}
