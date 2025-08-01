using System.Runtime.ConstrainedExecution;
using Meditrans.Shared.DbContexts;
using Meditrans.Shared.DTOs;
using Meditrans.Shared.Entities;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Meditrans.Api.Services
{
    public class FundingSourceBillingItemService : IFundingSourceBillingItemService
    {
        private readonly RaphaelContext _context;

        public FundingSourceBillingItemService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<FundingSourceBillingItem>> GetAllAsync()
        {
            return await _context.FundingSourceBillingItems
                .Include(f => f.FundingSource)
                .Include(f => f.BillingItem)
                .Include(f => f.SpaceType)
                .ToListAsync();
        }

        public async Task<FundingSourceBillingItemDto?> GetByIdAsync(int id)
        {
            var f = await _context.FundingSourceBillingItems
                .Include(f => f.FundingSource)
                .Include(f => f.BillingItem)
                .Include(f => f.SpaceType)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (f == null) return null;

            return new FundingSourceBillingItemDto
            {
                FundingSourceId = f.FundingSourceId,
                BillingItemId = f.BillingItemId,    
                SpaceTypeId = f.SpaceTypeId,
                Rate = f.Rate,
                Per = f.Per,
                IsDefault = f.IsDefault,     
                ProcedureCode = f.ProcedureCode,    
                MinCharge = f.MinCharge,    
                MaxCharge = f.MaxCharge,    
                GreaterThanMinQty = f.GreaterThanMinQty,    
                LessOrEqualMaxQty = f.LessOrEqualMaxQty,
                FreeQty = f.FreeQty,    
                FromDate = f.FromDate,  
                ToDate = f.ToDate        
            };
        }

        public async Task<FundingSourceBillingItemDto> CreateAsync(FundingSourceBillingItemDto dto)
        {
            var fsbi = new FundingSourceBillingItem
            {
                FundingSourceId = dto.FundingSourceId,
                BillingItemId = dto.BillingItemId,
                SpaceTypeId = dto.SpaceTypeId,
                Rate = dto.Rate,
                Per = dto.Per,
                IsDefault = dto.IsDefault,
                ProcedureCode = dto.ProcedureCode,
                MinCharge = dto.MinCharge,
                MaxCharge = dto.MaxCharge,
                GreaterThanMinQty = dto.GreaterThanMinQty,
                LessOrEqualMaxQty = dto.LessOrEqualMaxQty,
                FreeQty = dto.FreeQty,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate
            };

            _context.FundingSourceBillingItems.Add(fsbi);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(fsbi.Id) ?? throw new Exception("FundingSourceBillingItem creation failed.");
        }

        public async Task<bool> UpdateAsync(int id, FundingSourceBillingItemDto dto)
        {
            var fsbi = await _context.FundingSourceBillingItems.FindAsync(id);
            if (fsbi == null) return false;

            fsbi.FundingSourceId = dto.FundingSourceId;
            fsbi.BillingItemId = dto.BillingItemId;
            fsbi.SpaceTypeId = dto.SpaceTypeId;
            fsbi.Rate = dto.Rate;
            fsbi.Per = dto.Per;
            fsbi.IsDefault = dto.IsDefault;
            fsbi.ProcedureCode = dto.ProcedureCode;
            fsbi.MinCharge = dto.MinCharge;
            fsbi.MaxCharge = dto.MaxCharge;
            fsbi.GreaterThanMinQty = dto.GreaterThanMinQty;
            fsbi.LessOrEqualMaxQty = dto.LessOrEqualMaxQty;
            fsbi.FreeQty = dto.FreeQty;
            fsbi.FromDate = dto.FromDate;
            fsbi.ToDate = dto.ToDate;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var fsbi = await _context.FundingSourceBillingItems.FindAsync(id);
            if (fsbi == null) return false;

            _context.FundingSourceBillingItems.Remove(fsbi);
            await _context.SaveChangesAsync();
            return true;
        }

    }// end class    
}// end namespace
