using Microsoft.EntityFrameworkCore;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Catalog;

namespace Raphael.Api.Services.Catalog
{
    /// <inheritdoc cref="ICatalogLookupService"/>
    public sealed class CatalogLookupService : ICatalogLookupService
    {
        private readonly RaphaelContext _context;

        public CatalogLookupService(RaphaelContext context)
        {
            _context = context;
        }

        public async Task<IReadOnlyList<CatalogCategoryDto>> GetIntegratorCategoriesAsync(
            CancellationToken cancellationToken)
        {
            return await _context.CatalogIntegratorCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CatalogCategoryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    NameEn = c.NameEn,
                    NameEs = c.NameEs,
                    DisplayOrder = c.DisplayOrder,
                    Count = _context.CatalogIntegrators.Count(x => x.CategoryId == c.Id && x.IsActive)
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CatalogCategoryDto>> GetProviderCategoriesAsync(
            CancellationToken cancellationToken)
        {
            return await _context.CatalogProviderCategories
                .AsNoTracking()
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new CatalogCategoryDto
                {
                    Id = c.Id,
                    Code = c.Code,
                    NameEn = c.NameEn,
                    NameEs = c.NameEs,
                    DisplayOrder = c.DisplayOrder,
                    Count = _context.CatalogProviders.Count(x => x.CategoryId == c.Id && x.IsActive)
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CountyDto>> GetCountiesAsync(
            string? state,
            bool inUseOnly,
            CancellationToken cancellationToken)
        {
            var query = _context.Counties.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(state))
            {
                var wanted = state.Trim();
                query = query.Where(c => c.State == wanted);
            }

            if (inUseOnly)
            {
                query = query.Where(c =>
                    _context.CatalogIntegrators.Any(x => x.CountyId == c.Id)
                    || _context.CatalogProviders.Any(x => x.CountyId == c.Id));
            }

            return await query
                .OrderBy(c => c.State)
                .ThenBy(c => c.Name)
                .Select(c => new CountyDto
                {
                    Id = c.Id,
                    State = c.State,
                    Name = c.Name
                })
                .ToListAsync(cancellationToken);
        }
    }
}
