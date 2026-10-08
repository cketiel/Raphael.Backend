using Raphael.Api.Services.Catalog;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.Entities;
using Raphael.Shared.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Raphael.Api.Services
{
    public class IntegratorService : IIntegratorService
    {
        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ICatalogAccountSync _catalogSync;

        public IntegratorService(RaphaelContext context, ICurrentUserService currentUser, ICatalogAccountSync catalogSync)
        {
            _context = context;
            _currentUser = currentUser;
            _catalogSync = catalogSync;
        }

        public async Task<IEnumerable<IntegratorDto>> GetAllAsync()
        {
            return await _context.Integrators
                .Include(i => i.FundingSource)
                .Select(i => new IntegratorDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    ApiKey = i.ApiKey,
                    IsActive = i.IsActive,
                    Created = i.Created,
                    FundingSourceId = i.FundingSourceId,
                    FundingSourceName = i.FundingSource != null ? i.FundingSource.Name : null,
                    Phone = i.Phone,
                    Website = i.Website,
                    Email = i.Email,
                    Address = i.Address,
                    ContactName = i.ContactName,
                    Comments = i.Comments,
                    Latitude = i.Latitude,
                    Longitude = i.Longitude,
                    CatalogIntegratorId = i.CatalogIntegratorId,
                    OwnerProviderId = i.OwnerProviderId
                }).ToListAsync();
        }

        public async Task<IntegratorDto?> GetByIdAsync(int id)
        {
            var i = await _context.Integrators.FindAsync(id);
            if (i == null) return null;
            return new IntegratorDto
            {
                Id = i.Id,
                Name = i.Name,
                ApiKey = i.ApiKey,
                IsActive = i.IsActive,
                Created = i.Created,
                FundingSourceId = i.FundingSourceId,
                FundingSourceName = i.FundingSource != null ? i.FundingSource.Name : null,
                Phone = i.Phone,
                Website = i.Website,
                Email = i.Email,
                Address = i.Address,
                ContactName = i.ContactName,
                Comments = i.Comments,
                Latitude = i.Latitude,
                Longitude = i.Longitude,
                CatalogIntegratorId = i.CatalogIntegratorId,
                OwnerProviderId = i.OwnerProviderId
            };
        }

        public async Task<IntegratorDto> CreateAsync(IntegratorDto dto)
        {
            var integrator = new Integrator
            {
                IsActive = true,
                Created = DateTime.UtcNow,
                ApiKey = GenerateKey(), // Automatic generation
                FundingSourceId = dto.FundingSourceId,
                Comments = dto.Comments,
                CatalogIntegratorId = dto.CatalogIntegratorId,

                // From the token, never from the request. A client that could name its own
                // owner could claim another company's entities as its own.
                OwnerProviderId = _currentUser.ProviderId
            };

            // Name, address and contact: the catalog's when the account comes out of it.
            await _catalogSync.SaveIntegratorIdentityAsync(integrator, integrator.CatalogIntegratorId, Identity(dto));

            _context.Integrators.Add(integrator);
            await _context.SaveChangesAsync();
            dto.Id = integrator.Id;
            dto.ApiKey = integrator.ApiKey;
            return dto;
        }

        public async Task<bool> UpdateAsync(int id, IntegratorDto dto)
        {
            var existing = await _context.Integrators.FindAsync(id);
            if (existing == null) return false;

            // The link to the catalog is set once, when the row is created out of it. An update
            // can fill it in if it was never set, but it cannot move a row to another entity:
            // that would rewrite where an integrator came from.
            var catalogId = existing.CatalogIntegratorId ?? dto.CatalogIntegratorId;

            // Name, address and contact go through the catalog when the account is linked to it.
            // First, before anything else on the account changes (see ICatalogAccountSync).
            await _catalogSync.SaveIntegratorIdentityAsync(existing, catalogId, Identity(dto));

            existing.CatalogIntegratorId = catalogId;
            existing.IsActive = dto.IsActive;
            existing.FundingSourceId = dto.FundingSourceId;
            existing.Comments = dto.Comments;

            if (dto.RegenerateApiKey)
            {
                existing.ApiKey = GenerateKey();
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var i = await _context.Integrators.FindAsync(id);
            if (i == null) return false;
            _context.Integrators.Remove(i);
            await _context.SaveChangesAsync();
            return true;
        }

        private static AccountIdentity Identity(IntegratorDto dto) =>
            new(dto.Name, dto.Address, dto.Phone, dto.Email, dto.Website, dto.ContactName, dto.Latitude, dto.Longitude);

        private string GenerateKey()
        {
            // Format: itg_ + long random string
            return "itg_" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        public async Task<FundingSource?> GetFundingSourceByIntegratorIdAsync(int? integratorId)
        {
            var integrator = await _context.Integrators
                .Include(i => i.FundingSource)
                .FirstOrDefaultAsync(i => i.Id == integratorId);

            return integrator?.FundingSource;
        }
    }
}