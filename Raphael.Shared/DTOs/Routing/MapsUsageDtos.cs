using System;
using System.Collections.Generic;

namespace Raphael.Shared.DTOs.Routing
{
    /// <summary>
    /// What the administrator's Google Maps panel reads. Desktop keeps a hand-written copy.
    /// </summary>
    public static class MapsUsageContract
    {
        /// <summary>
        /// Names of <see cref="Raphael.Shared.Routing.MapsSku"/> as they cross the wire, so the
        /// panel does not depend on the numeric values of an enum it cannot see.
        /// </summary>
        public static class Skus
        {
            public const string RoutesEssentials = "RoutesEssentials";
            public const string RoutesPro = "RoutesPro";
            public const string Geocoding = "Geocoding";
            public const string DynamicMaps = "DynamicMaps";
            public const string PlacesAutocomplete = "PlacesAutocomplete";
            public const string PlaceDetails = "PlaceDetails";
        }
    }

    /// <summary>One product's figures over the period asked for.</summary>
    public class MapsSkuUsageDto
    {
        /// <summary>One of <see cref="MapsUsageContract.Skus"/>.</summary>
        public string Sku { get; set; } = string.Empty;

        /// <summary>What it is called on a Google invoice.</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>Requests that went to Google and cost money.</summary>
        public long Billed { get; set; }

        /// <summary>Requests our own cache answered for nothing.</summary>
        public long Cached { get; set; }

        /// <summary>
        /// True when this SKU is counted from what a client told us rather than measured on the
        /// server. The map, the autocomplete and the pin-drag geocoder go straight from a
        /// WebView to Google and this server never sees them.
        /// </summary>
        public bool ReportedByClient { get; set; }

        /// <summary>Estimated dollars for <see cref="Billed"/> at Google's volume bands.</summary>
        public decimal EstimatedCost { get; set; }

        /// <summary>
        /// What <see cref="Cached"/> would have cost had the cache not answered them — the saving,
        /// priced at the band the traffic would actually have fallen into.
        /// </summary>
        public decimal AvoidedCost { get; set; }

        /// <summary>Free requests Google allows per month for this SKU.</summary>
        public int FreeCapPerMonth { get; set; }

        /// <summary>
        /// How much of the free allowance is left this calendar month. What an administrator sets
        /// a Cloud Console quota against.
        /// </summary>
        public long FreeRemainingThisMonth { get; set; }
    }

    /// <summary>The period's headline figures.</summary>
    public class MapsUsageSummaryDto
    {
        public DateTime From { get; set; }

        public DateTime To { get; set; }

        public long TotalBilled { get; set; }

        public long TotalCached { get; set; }

        /// <summary>Share of all requests the cache answered, 0 to 1.</summary>
        public double CacheHitRate { get; set; }

        public decimal EstimatedCost { get; set; }

        public decimal AvoidedCost { get; set; }

        /// <summary>
        /// Where the month is heading at the current daily rate. Null unless the period covers
        /// part of the current month — projecting a period that has already closed is arithmetic
        /// dressed up as a forecast.
        /// </summary>
        public decimal? ProjectedMonthCost { get; set; }

        public List<MapsSkuUsageDto> BySku { get; set; } = new();
    }

    /// <summary>One day of one product, for the charts.</summary>
    public class MapsUsagePointDto
    {
        public DateTime Day { get; set; }

        public string Sku { get; set; } = string.Empty;

        public long Billed { get; set; }

        public long Cached { get; set; }
    }

    /// <summary>Everything ever counted, with no date filter at all.</summary>
    public class MapsUsageTotalsDto
    {
        public long Billed { get; set; }

        public long Cached { get; set; }

        public double CacheHitRate { get; set; }

        /// <summary>First and last day with any traffic. Null when nothing has been counted.</summary>
        public DateTime? FirstDay { get; set; }

        public DateTime? LastDay { get; set; }
    }

    /// <summary>A pricing band, as the configuration table shows it.</summary>
    public class MapsPricingTierDto
    {
        public int Id { get; set; }

        public string Sku { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public int FreeCapPerMonth { get; set; }

        public int FromRequest { get; set; }

        public int? ToRequest { get; set; }

        public decimal PricePerThousand { get; set; }
    }

    /// <summary>
    /// What a client reports after making its own Google calls.
    /// </summary>
    /// <remarks>
    /// Sent by the Desktop on behalf of its map pages. Those calls carry the browser key and go
    /// straight to Google, so without this the panel would be blind to a third of the bill.
    /// </remarks>
    public class MapsUsageReportDto
    {
        public List<MapsUsageReportItemDto> Items { get; set; } = new();
    }

    public class MapsUsageReportItemDto
    {
        /// <summary>One of <see cref="MapsUsageContract.Skus"/>.</summary>
        public string Sku { get; set; } = string.Empty;

        public int Count { get; set; } = 1;
    }
}
