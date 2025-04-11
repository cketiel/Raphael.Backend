namespace Meditrans.Shared.Entities
{
    public class SpaceType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int LoadTime { get; set; }
        public int UnloadTime { get; set; }
        public int CapacityTypeId { get; set; }
        public CapacityType CapacityType { get; set; }
        public bool IsActive { get; set; }
        public ICollection<Customer> Customers { get; set; }
        public ICollection<Trip> Trips { get; set; }
        public ICollection<CapacityDetail> CapacityDetails { get; set; }
        public ICollection<FundingSourceBillingItem> FundingSourceBillingItems { get; set; }
    }
}
