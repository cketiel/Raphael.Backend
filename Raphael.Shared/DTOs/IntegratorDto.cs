using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class IntegratorDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "The Name is required")]
        public string Name { get; set; }
        public string? ApiKey { get; set; }
        public bool IsActive { get; set; }
        public DateTime Created { get; set; }
        // Extra property to know if the client wants to refresh the key
        public bool RegenerateApiKey { get; set; }

        public int? FundingSourceId { get; set; }
        public string? FundingSourceName { get; set; }

        // ---- Contact details, added in RE-027 ----------------------------------------------

        [StringLength(40)]
        public string? Phone { get; set; }

        [StringLength(300)]
        public string? Website { get; set; }

        [StringLength(200)]
        public string? Email { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [StringLength(200)]
        public string? ContactName { get; set; }

        public string? Comments { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        // ---- Link to the catalog -------------------------------------------------------------

        /// <summary>
        /// The catalog row this one was added from. Sent by the client when it creates an
        /// integrator out of the catalog, so the catalog can mark the entity as already in
        /// this company.
        /// </summary>
        public int? CatalogIntegratorId { get; set; }

        /// <summary>
        /// Which company owns this row. Read-only from the client's point of view: the server
        /// takes it from the token, because a client that could set it could claim another
        /// company's entities as its own.
        /// </summary>
        public int? OwnerProviderId { get; set; }

        public string? OwnerProviderName { get; set; }
    }
}
