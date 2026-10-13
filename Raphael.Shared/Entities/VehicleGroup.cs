using Raphael.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class VehicleGroup : IOwnedByProvider
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        //public ICollection<Vehicle> Vehicles { get; set; }

        /// <summary>
        /// The client company (tenant) this row belongs to. <c>null</c> means Raphael's own
        /// operation, the general broker, as in <c>User.ProviderId</c> (RE-049).
        /// </summary>
        public int? OwnerProviderId { get; set; }
    }
}

