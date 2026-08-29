using Raphael.Shared.Entities.Routing;
using Raphael.Shared.Routing;

namespace Raphael.Api.Services.Routing
{
    /// <summary>
    /// Turns a month's request count into dollars, the way Google's volume bands do.
    /// </summary>
    /// <remarks>
    /// Google prices each SKU on its own: a monthly free allowance, then progressively cheaper
    /// bands as the volume grows. The bands apply to the request's position in the month, so the
    /// hundred-thousandth request is cheaper than the eleven-thousandth, and the answer is the
    /// sum of the slices — not the whole volume at whichever band the total lands in.
    ///
    /// <para>
    /// ⚠️ Everything here is an estimate, and the panel says so. Three reasons, all of them real:
    /// the bands are monthly, so any range that is not a calendar month prices a volume Google
    /// never saw; the bands are per Cloud project, so another application sharing the key eats
    /// the same free cap; and a request Google rejects is usually not billed, while we count what
    /// we sent.
    /// </para>
    /// </remarks>
    public static class MapsPricingCalculator
    {
        /// <summary>
        /// What <paramref name="requests"/> cost in a month that already contains
        /// <paramref name="alreadyUsed"/> of them.
        /// </summary>
        /// <remarks>
        /// <paramref name="alreadyUsed"/> is what makes the saving figure honest. Asking what the
        /// cache saved is asking what those requests would have cost <b>on top of</b> the ones we
        /// really made — they would have landed in a higher band, not started again from the free
        /// allowance.
        /// </remarks>
        public static decimal Cost(
            IReadOnlyList<MapsPricingTier> tiers,
            MapsSku sku,
            long requests,
            long alreadyUsed = 0)
        {
            if (requests <= 0) return 0m;

            var bands = tiers
                .Where(t => t.Sku == sku)
                .OrderBy(t => t.FromRequest)
                .ToList();

            if (bands.Count == 0) return 0m;

            var freeCap = bands[0].FreeCapPerMonth;

            // The window this batch occupies in the month, as 1-based request positions.
            var windowStart = alreadyUsed + 1;
            var windowEnd = alreadyUsed + requests;

            // Nothing before the free allowance runs out costs anything.
            windowStart = Math.Max(windowStart, freeCap + 1);

            if (windowStart > windowEnd) return 0m;

            var total = 0m;

            foreach (var band in bands)
            {
                var bandStart = (long)band.FromRequest;
                var bandEnd = band.ToRequest.HasValue ? band.ToRequest.Value : long.MaxValue;

                var from = Math.Max(windowStart, bandStart);
                var to = Math.Min(windowEnd, bandEnd);

                if (from > to) continue;

                total += (to - from + 1) / 1000m * band.PricePerThousand;
            }

            return Math.Round(total, 2);
        }

        /// <summary>The free monthly allowance for a SKU, or zero when nothing is configured.</summary>
        public static int FreeCap(IReadOnlyList<MapsPricingTier> tiers, MapsSku sku) =>
            tiers.Where(t => t.Sku == sku).Select(t => t.FreeCapPerMonth).FirstOrDefault();

        /// <summary>The name this SKU carries on a Google invoice.</summary>
        public static string DisplayName(MapsSku sku) => sku switch
        {
            MapsSku.RoutesEssentials => "Routes: Compute Routes Essentials",
            MapsSku.RoutesPro => "Routes: Compute Routes Pro",
            MapsSku.Geocoding => "Geocoding",
            MapsSku.DynamicMaps => "Dynamic Maps",
            MapsSku.PlacesAutocomplete => "Places: Autocomplete Requests",
            MapsSku.PlaceDetails => "Places: Place Details Essentials",
            _ => sku.ToString()
        };

        /// <summary>
        /// True when this SKU's tally comes from a client's own report rather than from a call
        /// this server made.
        /// </summary>
        public static bool IsReportedByClient(MapsSku sku) =>
            sku is MapsSku.DynamicMaps or MapsSku.PlacesAutocomplete or MapsSku.PlaceDetails;
    }
}
