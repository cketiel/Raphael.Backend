namespace Meditrans.Shared.Entities
{
    public class Unit
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public ICollection<BillingItem> BillingItems { get; set; }
    }
}
