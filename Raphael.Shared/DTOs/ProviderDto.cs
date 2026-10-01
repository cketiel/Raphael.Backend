using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Raphael.Shared.DTOs
{
    public class ProviderDto
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "The provider name is required.")]
        [StringLength(150)]
        public string Name { get; set; }

        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Logo { get; set; }
        public IFormFile? LogoFile { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>
        /// IANA identifier of the timezone this provider's trips are operated in.
        /// </summary>
        /// <remarks>
        /// ⚠️ The column has existed on the entity since the timezone work, but until RE-027 it
        /// was on neither side of this DTO: the API never sent it and never saved it. The
        /// Providers screen has a whole column built to flag providers that have not declared
        /// one, and it was flagging every single provider, with no way to answer it.
        /// </remarks>
        [StringLength(64)]
        public string? TimeZoneId { get; set; }

        // ---- Contact details, added in RE-027 ----------------------------------------------

        [StringLength(300)]
        public string? Website { get; set; }

        [StringLength(200)]
        public string? ContactName { get; set; }

        public string? Comments { get; set; }

        // ---- Link to the catalog -------------------------------------------------------------

        /// <summary>
        /// The catalog row this one was added from. Sent by the client when it creates a
        /// provider out of the catalog, so the catalog can mark the entity as already in this
        /// company.
        /// </summary>
        public int? CatalogProviderId { get; set; }

        /// <summary>
        /// Which company owns this row. Read-only from the client's point of view: the server
        /// takes it from the token.
        /// </summary>
        public int? OwnerProviderId { get; set; }

        public string? OwnerProviderName { get; set; }
    }
}
