namespace Meditrans.Shared.Entities
{
    public class Customer
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Zip { get; set; }
        public string Phone { get; set; }
        public string MobilePhone { get; set; }
        public string ClientCode { get; set; }
        public string PolicyNumber { get; set; }
        public int FundingSourceId { get; set; }
        public FundingSource FundingSource { get; set; }
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        public string Email { get; set; }
        public DateTime DOB { get; set; }
        public DateTime Created { get; set; }
        public string CreatedBy { get; set; }
        public ICollection<Trip> Trips { get; set; }
    }
}
