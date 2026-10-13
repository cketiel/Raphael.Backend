using Raphael.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class VehicleType : IOwnedByProvider
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }

        /// <summary>
        /// The client company (tenant) that added this entry for itself. <c>null</c> means the
        /// common catalog every tenant reads and only the platform edits (RE-049, decision M5).
        /// </summary>
        public int? OwnerProviderId { get; set; }
    }
}

