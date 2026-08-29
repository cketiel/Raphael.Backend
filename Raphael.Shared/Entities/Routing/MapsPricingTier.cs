using Raphael.Shared.Routing;

namespace Raphael.Shared.Entities.Routing
{
    /// <summary>
    /// One band of Google's volume pricing for one product.
    /// </summary>
    /// <remarks>
    /// In the database rather than in code on purpose. Google restructured this whole table in
    /// March 2025 — replacing a flat $200 credit with per-SKU free caps — and will do it again.
    /// A price change should be an UPDATE an administrator can make the morning they read the
    /// announcement, not a release.
    ///
    /// <para>
    /// ⚠️ The bands are <b>monthly and per project</b>, which is what makes any figure this table
    /// produces an estimate. A date range that is not a calendar month prices a volume Google
    /// would never have seen, and if another application shares the key its traffic counts toward
    /// the same free cap and the same bands. The panel says so; so should anyone reading it.
    /// </para>
    /// </remarks>
    public class MapsPricingTier
    {
        public int Id { get; set; }

        public MapsSku Sku { get; set; }

        /// <summary>
        /// Requests in the month that are free before this SKU costs anything. Repeated on every
        /// band of a SKU so a single row carries everything the calculator needs.
        /// </summary>
        public int FreeCapPerMonth { get; set; }

        /// <summary>First request of the month this band applies to, counting from one.</summary>
        public int FromRequest { get; set; }

        /// <summary>Last request this band applies to; null for the open-ended top band.</summary>
        public int? ToRequest { get; set; }

        /// <summary>US dollars per thousand requests inside this band.</summary>
        public decimal PricePerThousand { get; set; }
    }
}
