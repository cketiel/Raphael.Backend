using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class BillingItem
    {
        public int Id { get; set; }
        [Required]
        public string Description { get; set; }
        public int UnitId { get; set; }
        public Unit Unit { get; set; }
        public bool IsCopay { get; set; }
        public string? ARAccount { get; set; }
        public string? ARSubAccount { get; set; }
        public string? ARCompany { get; set; }
        public string? APAccount { get; set; }
        public string? APSubAccount { get; set; }
        public string? APCompany { get; set; }
        public ICollection<FundingSourceBillingItem> FundingSourceBillingItems { get; set; }

        /// <summary>
        /// The clinic that keeps this item through the Booking Portal; null for the office's own.
        /// </summary>
        /// <remarks>
        /// A clinic never sees the office's list. It defines the items it bills with and rates them
        /// on its funding source, and the office sees them grouped under that clinic (BOOKING_ADMIN.md §4).
        /// </remarks>
        public int? OwnerIntegratorId { get; set; }

        public Integrator? OwnerIntegrator { get; set; }
    }
}

