using System.ComponentModel.DataAnnotations;
using Raphael.Shared.Entities.Catalog;

namespace Raphael.Shared.Entities
{
    public class Provider
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Logo { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>
        /// The timezone this provider's trips are operated in. IANA identifier.
        /// </summary>
        /// <remarks>
        /// This is what a pickup time means. A trip at 09:15 is 09:15 here, whoever opens the
        /// screen and wherever the server happens to be hosted.
        ///
        /// <para>
        /// Nullable because the column arrived after the rows did. A provider that has not
        /// declared one falls back to the configured default, never to the server's own
        /// timezone — see <c>OperationTimeOptions</c>. The Providers screen flags the ones
        /// still empty, so the fallback does not become permanent by inattention.
        /// </para>
        /// </remarks>
        [MaxLength(64)]
        public string? TimeZoneId { get; set; }

        // ---- Contact details -------------------------------------------------------------
        // Added in RE-027, to complete what the provider row already had: address, email,
        // phone and coordinates were here; these three were not.

        [MaxLength(300)]
        public string? Website { get; set; }

        [MaxLength(200)]
        public string? ContactName { get; set; }

        public string? Comments { get; set; }

        // ---- Where this one came from ------------------------------------------------------

        /// <summary>
        /// The catalog row this provider was added from, when it was added from the catalog.
        /// Null for the ones that predate the catalog or were created by hand.
        /// </summary>
        public int? CatalogProviderId { get; set; }
        public CatalogProvider? CatalogProvider { get; set; }

        /// <summary>
        /// The company this row belongs to. <c>null</c> means the general broker, the same reading
        /// as <c>User.ProviderId</c>. See <c>Integrator.OwnerProviderId</c>.
        /// </summary>
        public int? OwnerProviderId { get; set; }
    }
}

