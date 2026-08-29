using Raphael.Shared.Routing;
using System;

namespace Raphael.Shared.Entities.Routing
{
    /// <summary>
    /// One day's tally of Google Maps requests, split by product and by whether we paid for it.
    /// </summary>
    /// <remarks>
    /// Counters, not events. One row per request would be honest and useless: the routing proxy
    /// answers tens of thousands of legs a day, almost all of them from cache, and nobody will
    /// ever ask which second a particular cache hit happened. A daily counter answers every
    /// question the administrator actually has — what did we spend, what did we avoid, is the
    /// cache winning — in six SKUs times two outcomes times 365, about four thousand rows a year.
    ///
    /// <para>
    /// <see cref="Day"/> is the operation's own business date, not UTC: an administrator comparing
    /// this against a Google invoice is thinking in the working day, and a route recalculated at
    /// eight in the evening belongs to that evening.
    /// </para>
    ///
    /// <para>
    /// ⚠️ Rows for <see cref="MapsSku.DynamicMaps"/>, <see cref="MapsSku.PlacesAutocomplete"/> and
    /// <see cref="MapsSku.PlaceDetails"/> are <b>reported by the clients</b>, not measured here:
    /// those calls go straight from a WebView to Google and this server never sees them. They are
    /// as accurate as the reporting, which is close but not the invoice.
    /// </para>
    /// </remarks>
    public class MapsUsageDaily
    {
        public long Id { get; set; }

        /// <summary>The business date, at midnight. No time component is ever stored.</summary>
        public DateTime Day { get; set; }

        public MapsSku Sku { get; set; }

        /// <summary>
        /// True when the request went to Google and cost money; false when our own cache answered
        /// it. This split is the entire point of the table.
        /// </summary>
        public bool Billed { get; set; }

        /// <summary>How many. Incremented in place, never one row per request.</summary>
        public int Count { get; set; }
    }
}
