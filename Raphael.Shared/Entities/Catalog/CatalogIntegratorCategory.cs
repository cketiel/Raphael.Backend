using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>
    /// The group a catalog integrator belongs to: nursing home, assisted living, hospital.
    /// </summary>
    /// <remarks>
    /// A table and not an enum on purpose. The groups come from how the source files are split and
    /// that split will change; adding one has to be an INSERT, not a release.
    /// </remarks>
    public class CatalogIntegratorCategory
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
