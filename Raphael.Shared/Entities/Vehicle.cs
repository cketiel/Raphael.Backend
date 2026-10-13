using Raphael.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class Vehicle : IOwnedByProvider
    {
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public string? VIN { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public int? Year { get; set; }
        public string? Plate { get; set; } // MAS Requirements
        public DateTime? ExpirationDate { get; set; } // MAS Requirements
        public bool IsInactive { get; set; }
        [Required]
        public int GroupId { get; set; }
        public VehicleGroup VehicleGroup { get; set; }
        [Required]
        public int CapacityDetailTypeId { get; set; }
        public CapacityDetailType CapacityDetailType { get; set; }
        [Required]
        public int VehicleTypeId { get; set; }
        public VehicleType VehicleType { get; set; }

        //public ICollection<VehicleRoute> VehicleRoutes { get; set; }

        /// <summary>
        /// The client company (tenant) this row belongs to. <c>null</c> means Raphael's own
        /// operation, the general broker, as in <c>User.ProviderId</c> (RE-049).
        /// </summary>
        public int? OwnerProviderId { get; set; }
    }
}

