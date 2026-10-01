using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>
    /// The group a catalog provider belongs to: NEMT company, NEMT broker, private ambulance.
    /// </summary>
    /// <remarks>See <see cref="CatalogIntegratorCategory"/> for why this is a table.</remarks>
    public class CatalogProviderCategory
    {
        public int Id { get; set; }

        /// <summary>Stable identifier used by code and by the import templates. Never translated.</summary>
        [Required]
        [MaxLength(40)]
        public string Code { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameEn { get; set; }

        [Required]
        [MaxLength(100)]
        public string NameEs { get; set; }

        public int DisplayOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
