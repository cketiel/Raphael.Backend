using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Catalog;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services.Catalog
{
    /// <summary>The clinic, Provider or catalog row does not exist: answered 404.</summary>
    public sealed class OfficeRelationsNotFoundException : Exception
    {
    }

    /// <summary>What was asked would leave the lists wrong: answered 400 with the reason.</summary>
    public sealed class OfficeRelationsRuleException : Exception
    {
        public OfficeRelationsRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// The office's view and correction of who works with whom (PROVIDER_ACCESS_MODEL.md §3.6): the
    /// Providers a clinic contracted, the clinics that contracted a Provider, and the clinic's row in
    /// the catalog. One relation (IntegratorProviders), seen from both sides. Office admins only: the
    /// controllers say so.
    /// </summary>
    public interface IOfficeRelationsService
    {
        Task<IntegratorRelationsDto> GetIntegratorRelationsAsync(int integratorId, CancellationToken ct);

        /// <summary>Contracts a catalog Provider on the clinic's behalf, and gives it its account, as Booking does.</summary>
        Task<IntegratorRelationsDto> AddProviderAsync(int integratorId, int catalogProviderId, CancellationToken ct);

        Task<IntegratorRelationsDto> RemoveProviderAsync(int integratorId, int catalogProviderId, CancellationToken ct);

        Task<IntegratorRelationsDto> LinkCatalogAsync(int integratorId, int? catalogIntegratorId, CancellationToken ct);

        Task<List<ProviderClinicRowDto>> GetProviderClinicsAsync(int providerId, CancellationToken ct);
    }

    public sealed class OfficeRelationsService : IOfficeRelationsService
    {
        private readonly RaphaelContext _context;
        private readonly ICatalogAccountSync _sync;
        private readonly ICurrentUserService _currentUser;

        public OfficeRelationsService(RaphaelContext context, ICatalogAccountSync sync, ICurrentUserService currentUser)
        {
            _context = context;
            _sync = sync;
            _currentUser = currentUser;
        }

        /// <summary>
        /// The office only: role 1 is not enough, an internal Provider's admin has it too, and these
        /// lists are every clinic's. Answered as a 404, as the clinic endpoints do.
        /// </summary>
        private void OfficeOnly()
        {
            if (!_currentUser.IsMilanesInternal) throw new OfficeRelationsNotFoundException();
        }

        public async Task<IntegratorRelationsDto> GetIntegratorRelationsAsync(int integratorId, CancellationToken ct)
        {
            OfficeOnly();
            var clinic = await _context.Integrators.AsNoTracking()
                .Where(i => i.Id == integratorId)
                .Select(i => new { i.Id, i.CatalogIntegratorId, CatalogName = i.CatalogIntegrator != null ? i.CatalogIntegrator.Name : null })
                .FirstOrDefaultAsync(ct) ?? throw new OfficeRelationsNotFoundException();

            var providers = await _context.IntegratorProviders.AsNoTracking()
                .Where(r => r.IntegratorId == integratorId)
                .Select(r => new IntegratorProviderRowDto
                {
                    CatalogProviderId = r.CatalogProviderId,
                    Name = r.CatalogProvider.Name,
                    AccountId = _context.Providers.Where(p => p.CatalogProviderId == r.CatalogProviderId).OrderBy(p => p.Id).Select(p => (int?)p.Id).FirstOrDefault(),
                    WorkMode = _context.Providers.Where(p => p.CatalogProviderId == r.CatalogProviderId).OrderBy(p => p.Id).Select(p => p.WorkMode.ToString()).FirstOrDefault(),
                    ActiveUsers = _context.Users.Count(u => u.IsActive && u.Provider != null && u.Provider.CatalogProviderId == r.CatalogProviderId),
                    ContractedAtUtc = r.CreatedAtUtc
                })
                .OrderBy(r => r.Name)
                .ToListAsync(ct);

            return new IntegratorRelationsDto
            {
                IntegratorId = clinic.Id,
                CatalogIntegratorId = clinic.CatalogIntegratorId,
                CatalogName = clinic.CatalogName,
                Providers = providers
            };
        }

        public async Task<IntegratorRelationsDto> AddProviderAsync(int integratorId, int catalogProviderId, CancellationToken ct)
        {
            OfficeOnly();
            if (!await _context.Integrators.AnyAsync(i => i.Id == integratorId, ct)) throw new OfficeRelationsNotFoundException();
            var row = await _context.CatalogProviders.AsNoTracking().Where(c => c.Id == catalogProviderId).Select(c => new { c.IsActive }).FirstOrDefaultAsync(ct)
                ?? throw new OfficeRelationsNotFoundException();
            if (!row.IsActive) throw new OfficeRelationsRuleException("This provider is no longer active in the catalog.");

            await _sync.EnsureProviderAccountAsync(catalogProviderId, ct);
            if (!await _context.IntegratorProviders.AnyAsync(r => r.IntegratorId == integratorId && r.CatalogProviderId == catalogProviderId, ct))
            {
                _context.IntegratorProviders.Add(new IntegratorProvider
                {
                    IntegratorId = integratorId,
                    CatalogProviderId = catalogProviderId,
                    CreatedAtUtc = DateTime.UtcNow,
                    CreatedByUserId = _currentUser.UserId
                });
            }
            await _context.SaveChangesAsync(ct);
            return await GetIntegratorRelationsAsync(integratorId, ct);
        }

        /// <remarks>Trips already given to this Provider keep it: removing it only stops new ones, as in Booking.</remarks>
        public async Task<IntegratorRelationsDto> RemoveProviderAsync(int integratorId, int catalogProviderId, CancellationToken ct)
        {
            OfficeOnly();
            var link = await _context.IntegratorProviders.FirstOrDefaultAsync(r => r.IntegratorId == integratorId && r.CatalogProviderId == catalogProviderId, ct);
            if (link is not null)
            {
                _context.IntegratorProviders.Remove(link);
                await _context.SaveChangesAsync(ct);
            }
            return await GetIntegratorRelationsAsync(integratorId, ct);
        }

        /// <remarks>
        /// One account per real clinic: a catalog row already linked to another clinic's account is
        /// refused, so two accounts never share one identity by mistake.
        /// </remarks>
        public async Task<IntegratorRelationsDto> LinkCatalogAsync(int integratorId, int? catalogIntegratorId, CancellationToken ct)
        {
            OfficeOnly();
            var clinic = await _context.Integrators.FirstOrDefaultAsync(i => i.Id == integratorId, ct) ?? throw new OfficeRelationsNotFoundException();
            if (catalogIntegratorId is int rowId)
            {
                if (!await _context.CatalogIntegrators.AnyAsync(c => c.Id == rowId, ct)) throw new OfficeRelationsNotFoundException();
                var other = await _context.Integrators.Where(i => i.Id != integratorId && i.CatalogIntegratorId == rowId).Select(i => i.Name).FirstOrDefaultAsync(ct);
                if (other is not null) throw new OfficeRelationsRuleException($"That catalog entry already belongs to {other}.");
            }
            clinic.CatalogIntegratorId = catalogIntegratorId;
            await _context.SaveChangesAsync(ct);
            return await GetIntegratorRelationsAsync(integratorId, ct);
        }

        public async Task<List<ProviderClinicRowDto>> GetProviderClinicsAsync(int providerId, CancellationToken ct)
        {
            OfficeOnly();
            var catalogId = await _context.Providers.Where(p => p.Id == providerId).Select(p => (int?)p.CatalogProviderId).FirstOrDefaultAsync(ct);
            if (catalogId is null) return new List<ProviderClinicRowDto>();

            return await _context.IntegratorProviders.AsNoTracking()
                .Where(r => r.CatalogProviderId == catalogId)
                .Join(_context.Integrators, r => r.IntegratorId, i => i.Id, (r, i) => new ProviderClinicRowDto
                {
                    IntegratorId = i.Id,
                    Name = i.Name,
                    IsActive = i.IsActive,
                    ContractedAtUtc = r.CreatedAtUtc
                })
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }
    }
}
