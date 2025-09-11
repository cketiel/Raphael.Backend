using System.Runtime.ConstrainedExecution;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Microsoft.AspNetCore.Server.IISIntegration;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Raphael.Shared.Dtos;

namespace Raphael.Api.Services
{
    public class FundingSourceBillingItemService : IFundingSourceBillingItemService
    {
        private readonly RaphaelContext _context;

        public FundingSourceBillingItemService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<List<FundingSourceBillingItemGetDto>> GetByFundingSourceIdAsync(int fundingSourceId, bool includeExpired)
        {
            var query = _context.FundingSourceBillingItems
                .Where(i => i.FundingSourceId == fundingSourceId)
                .Include(f => f.BillingItem)
                    .ThenInclude(bi => bi.Unit)
                .Include(f => f.SpaceType)
                .AsQueryable();

            if (!includeExpired)
            {
                query = query.Where(i => i.ToDate.Date >= DateTime.Today.Date);
            }
           
            return await query.Select(i => new FundingSourceBillingItemGetDto
            {
                Id = i.Id,
                FundingSourceId = i.Id,
                BillingItemId = i.BillingItemId,
                SpaceTypeId = i.SpaceTypeId,
                Rate = i.Rate,
                Per = i.Per,
                IsDefault = i.IsDefault,
                ProcedureCode = i.ProcedureCode,
                MinCharge = i.MinCharge,
                MaxCharge = i.MaxCharge,
                GreaterThanMinQty = i.GreaterThanMinQty,
                LessOrEqualMaxQty = i.LessOrEqualMaxQty,
                FreeQty = i.FreeQty,
                FromDate = i.FromDate,
                ToDate = i.ToDate,
                BillingItemDescription = i.BillingItem.Description,
                BillingItemUnitAbbreviation = i.BillingItem.Unit.Abbreviation,
                SpaceTypeName = i.SpaceType.Name
            }).ToListAsync();
        }

        public async Task<List<FundingSourceBillingItemGetDto>> GetAllAsync()
        {
            return await _context.FundingSourceBillingItems
                .Include(f => f.BillingItem)
                    .ThenInclude(bi => bi.Unit)
                .Include(f => f.SpaceType)
                .Select(i => new FundingSourceBillingItemGetDto 
                {
                    Id = i.Id,
                    FundingSourceId = i.Id,
                    BillingItemId = i.BillingItemId,
                    SpaceTypeId = i.SpaceTypeId,
                    Rate = i.Rate,
                    Per = i.Per,
                    IsDefault = i.IsDefault,
                    ProcedureCode = i.ProcedureCode,
                    MinCharge = i.MinCharge,
                    MaxCharge = i.MaxCharge,
                    GreaterThanMinQty = i.GreaterThanMinQty,
                    LessOrEqualMaxQty = i.LessOrEqualMaxQty,
                    FreeQty = i.FreeQty,
                    FromDate = i.FromDate,
                    ToDate = i.ToDate,
                    BillingItemDescription = i.BillingItem.Description,
                    BillingItemUnitAbbreviation = i.BillingItem.Unit.Abbreviation,
                    SpaceTypeName = i.SpaceType.Name
                }).ToListAsync();
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

        public async Task<FundingSourceBillingItem> CreateAsync(FundingSourceBillingItemDto dto)
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
          
            await _context.Entry(fsbi).Reference(i => i.BillingItem).LoadAsync();
            
            if (fsbi.BillingItem != null)
            {
                await _context.Entry(fsbi.BillingItem).Reference(b => b.Unit).LoadAsync();
            }
            await _context.Entry(fsbi).Reference(i => i.SpaceType).LoadAsync();
            
            return fsbi;
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

