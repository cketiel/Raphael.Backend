using Raphael.Shared.Interfaces;
namespace Raphael.Shared.Entities
{
    public class Unit : IOwnedByProvider
    {
        public int Id { get; set; }
        public string Abbreviation { get; set; }
        public string Description { get; set; }
        public ICollection<BillingItem> BillingItems { get; set; }

        /// <summary>
        /// The client company (tenant) that added this entry for itself. <c>null</c> means the
        /// common catalog every tenant reads and only the platform edits (RE-049, decision M5).
        /// </summary>
        public int? OwnerProviderId { get; set; }
    }
}

