namespace Raphael.Shared.Routing
{
    /// <summary>
    /// The Google Maps products this ecosystem actually buys, one value per billable SKU.
    /// </summary>
    /// <remarks>
    /// These are Google's own billing units, not our internal operations, because the whole point
    /// of counting them is to arrive at the same number as the invoice. Two of them —
    /// <see cref="RoutesEssentials"/> and <see cref="RoutesPro"/> — are the same call made two
    /// ways, and the difference between them is a factor of two in price and half the free tier.
    ///
    /// <para>
    /// ⚠️ The values are persisted. Append new ones; never renumber.
    /// </para>
    /// </remarks>
    public enum MapsSku : byte
    {
        /// <summary>Routes API without traffic. What MaxSavings mode buys. $5/1000, 10k free.</summary>
        RoutesEssentials = 0,

        /// <summary>Routes API with traffic. What Precision mode buys. $10/1000, 5k free.</summary>
        RoutesPro = 1,

        /// <summary>
        /// Geocoding, forward and reverse alike — Google bills them as one SKU. $5/1000, 10k free.
        /// </summary>
        Geocoding = 2,

        /// <summary>
        /// A map drawn in a WebView. Billed per load, so opening the trip form is a purchase.
        /// $7/1000, 10k free.
        /// </summary>
        DynamicMaps = 3,

        /// <summary>An address autocomplete request. $2.83/1000, 10k free.</summary>
        PlacesAutocomplete = 4,

        /// <summary>
        /// Reading the fields of a chosen place — what runs when a dispatcher picks a suggestion.
        /// $5/1000, 10k free.
        /// </summary>
        PlaceDetails = 5
    }
}
