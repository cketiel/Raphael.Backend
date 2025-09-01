using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.Dtos;
using Raphael.Shared.Entities;

namespace Raphael.Api.Services
{
    public class BillingItemService
    {
        private readonly RaphaelContext _context;

        public BillingItemService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<BillingItem>> GetAllAsync()
        {
            // The 'Unit' entity is included so that the data reaches the WPF client complete
            return await _context.BillingItems
                .Include(b => b.Unit)
                .ToListAsync();
        }

        public async Task<BillingItem?> GetByIdAsync(int id)
        {
            return await _context.BillingItems
                .Include(b => b.Unit)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<BillingItem> CreateAsync(BillingItemDto dto)
        {
            var billingItem = new BillingItem
            {
                Description = dto.Description,
                UnitId = dto.UnitId,
                IsCopay = dto.IsCopay,
                ARAccount = dto.ARAccount,
                ARSubAccount = dto.ARSubAccount,
                ARCompany = dto.ARCompany,
                APAccount = dto.APAccount,
                APSubAccount = dto.APSubAccount,
                APCompany = dto.APCompany
            };

            _context.BillingItems.Add(billingItem);
            await _context.SaveChangesAsync();

            // We reload the object with the Unit entity included to return it complete
            return await GetByIdAsync(billingItem.Id);
        }

        public async Task<BillingItem?> UpdateAsync(int id, BillingItemDto dto)
        {
            var billingItem = await _context.BillingItems.FindAsync(id);
            if (billingItem == null)
            {
                return null; // The item was not found
            }

            // We map the properties of the DTO to the entity found
            billingItem.Description = dto.Description;
            billingItem.UnitId = dto.UnitId;
            billingItem.IsCopay = dto.IsCopay;
            billingItem.ARAccount = dto.ARAccount;
            billingItem.ARSubAccount = dto.ARSubAccount;
            billingItem.ARCompany = dto.ARCompany;
            billingItem.APAccount = dto.APAccount;
            billingItem.APSubAccount = dto.APSubAccount;
            billingItem.APCompany = dto.APCompany;

            await _context.SaveChangesAsync();

            // We reload the object with the Unit entity included to return it complete
            return await GetByIdAsync(billingItem.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var billingItem = await _context.BillingItems.FindAsync(id);
            if (billingItem == null)
            {
                return false; 
            }

            _context.BillingItems.Remove(billingItem);
            await _context.SaveChangesAsync();
            return true; // Successful deletion
        }
    }
}
