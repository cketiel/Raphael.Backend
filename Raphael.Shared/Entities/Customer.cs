using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string Address { get; set; }
        [Required]
        public string City { get; set; }
        [Required]
        public string State { get; set; }
        [Required]
        public string Zip { get; set; }
        public string? Phone { get; set; }
        public string? MobilePhone { get; set; }
        public string? ClientCode { get; set; }
        public string? PolicyNumber { get; set; }
        [Required]
        public int FundingSourceId { get; set; }
        public FundingSource FundingSource { get; set; }
        [Required]
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        public string? Email { get; set; }
        public DateTime? DOB { get; set; }
        [Required]
        public string Gender { get; set; }
        [Required]
        public DateTime Created { get; set; }
        [Required]
        public string CreatedBy { get; set; }
        public string? RiderId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public ICollection<Trip> Trips { get; set; }
    }
}

