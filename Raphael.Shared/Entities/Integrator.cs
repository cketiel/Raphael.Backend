using System.ComponentModel.DataAnnotations;
using Raphael.Shared.Entities.Catalog;

namespace Raphael.Shared.Entities
{
    public class Integrator
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } // Example: "Ryde Central"
        [Required]
        public string ApiKey { get; set; } // rc_live_...
        public bool IsActive { get; set; } = true;
        public DateTime Created { get; set; } = DateTime.UtcNow;
        public int? FundingSourceId { get; set; }
        public FundingSource? FundingSource { get; set; }

        // ---- Contact details -------------------------------------------------------------
        // Added in RE-027. An integrator used to be a name and a key, which is all the API
        // needs and nothing of what a person needs when they have to call them.

        [MaxLength(40)]
        public string? Phone { get; set; }

        [MaxLength(300)]
        public string? Website { get; set; }

        [MaxLength(200)]
        public string? Email { get; set; }

        [MaxLength(300)]
        public string? Address { get; set; }

        [MaxLength(200)]
        public string? ContactName { get; set; }

        public string? Comments { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // ---- Where this one came from ------------------------------------------------------

        /// <summary>
        /// The catalog row this integrator was added from, when it was added from the catalog.
        /// Null for the ones that predate the catalog or were created by hand.
        /// </summary>
        /// <remarks>
        /// This is also how the catalog screen knows a row is already in somebody's company: the
        /// catalog never loses an entity, so the mark has to live on this side.
        /// </remarks>
        public int? CatalogIntegratorId { get; set; }
        public CatalogIntegrator? CatalogIntegrator { get; set; }

        /// <summary>
        /// The company this row belongs to. <c>null</c> means the general broker, the same reading
        /// as <c>User.ProviderId</c> and <c>Trip.IntegratorId</c>.
        /// </summary>
        /// <remarks>
        /// A provider operating under the broker can add the same catalog entity the broker
        /// already added; that is two rows here, each with its own key, and each company sees
        /// only its own as "in my company".
        /// </remarks>
        public int? OwnerProviderId { get; set; }
    }
}
