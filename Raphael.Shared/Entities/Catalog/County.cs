using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Catalog
{
    /// <summary>A county, as the catalog uses them to group and to filter.</summary>
    /// <remarks>
    /// Seeded with the 67 counties of Florida. <see cref="State"/> exists from day one so that
    /// covering another state is inserting rows rather than migrating a table.
    /// </remarks>
    public class County
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(2)]
        public string State { get; set; }

        [Required]
        [MaxLength(60)]
        public string Name { get; set; }
    }
}
