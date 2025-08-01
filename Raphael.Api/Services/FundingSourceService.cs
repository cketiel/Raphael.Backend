using Meditrans.Shared.DbContexts;
using Meditrans.Shared.Dtos;
using Meditrans.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Meditrans.Api.Services
{
    public class FundingSourceService
    {
        private readonly RaphaelContext _context;

        public FundingSourceService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<FundingSource>> GetAllAsync()
        {
            return await _context.FundingSources.ToListAsync();
        }

        public async Task<FundingSource?> GetByIdAsync(int id)
        {
            return await _context.FundingSources.FindAsync(id);
        }

        public async Task<FundingSource> CreateAsync(FundingSourceDto dto)
        {
            var funding = new FundingSource
            {
                Name = dto.Name,
                AccountNumber = dto.AccountNumber,
                Address = dto.Address,
                Phone = dto.Phone,
                FAX = dto.FAX,
                Email = dto.Email,
                ContactFirst = dto.ContactFirst,
                ContactLast = dto.ContactLast,
                SignaturePickup = dto.SignaturePickup,
                SignatureDropoff = dto.SignatureDropoff,
                DriverSignaturePickup = dto.DriverSignaturePickup,
                DriverSignatureDropoff = dto.DriverSignatureDropoff,
                RequireOdometer = dto.RequireOdometer,
                BarcodeScanRequired = dto.BarcodeScanRequired,
                VectorcareFacilityId = dto.VectorcareFacilityId,
                IsActive = dto.IsActive
                
            };

            _context.FundingSources.Add(funding);
            await _context.SaveChangesAsync();
            return funding;
        }

        public async Task<FundingSource?> UpdateAsync(int id, FundingSourceDto dto)
        {
            var funding = await _context.FundingSources.FindAsync(id);
            if (funding == null) return null;

            funding.Name = dto.Name;
            funding.AccountNumber = dto.AccountNumber;
            funding.Address = dto.Address;
            funding.Phone = dto.Phone;
            funding.FAX = dto.FAX;
            funding.Email = dto.Email;
            funding.ContactFirst = dto.ContactFirst;
            funding.ContactLast = dto.ContactLast;
            funding.SignaturePickup = dto.SignaturePickup;
            funding.SignatureDropoff = dto.SignatureDropoff;
            funding.DriverSignaturePickup = dto.DriverSignaturePickup;
            funding.DriverSignatureDropoff = dto.DriverSignatureDropoff;
            funding.RequireOdometer = dto.RequireOdometer;
            funding.BarcodeScanRequired = dto.BarcodeScanRequired;
            funding.VectorcareFacilityId = dto.VectorcareFacilityId;
            funding.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return funding;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var funding = await _context.FundingSources.FindAsync(id);
            if (funding == null) return false;

            _context.FundingSources.Remove(funding);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
