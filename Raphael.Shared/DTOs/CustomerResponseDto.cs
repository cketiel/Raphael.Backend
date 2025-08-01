namespace Raphael.Shared.DTOs
{
    public class CustomerResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string? Phone { get; set; }
        public string? MobilePhone { get; set; }
        public string? ClientCode { get; set; }
        public string? PolicyNumber { get; set; }
        public string? Email { get; set; }
        public DateTime? DOB { get; set; }
        public int FundingSourceId { get; set; }
        public string FundingSourceName { get; set; }
        public int SpaceTypeId { get; set; }
        public string SpaceTypeName { get; set; }
        public string Gender { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public string? RiderId { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

