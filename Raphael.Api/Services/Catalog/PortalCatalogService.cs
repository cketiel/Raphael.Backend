using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Routing;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Catalog;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Helpers;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;

namespace Raphael.Api.Services.Catalog
{
    /// <summary>What a clinic asked for is not its to do: answered 403 (not an admin) or 404.</summary>
    public sealed class PortalCatalogDeniedException : Exception
    {
        public PortalCatalogDeniedException(bool notFound) => NotFound = notFound;

        public bool NotFound { get; }
    }

    /// <summary>A request the rules refuse, with the sentence that explains why.</summary>
    public sealed class PortalCatalogRuleException : Exception
    {
        public PortalCatalogRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// The catalog as the Booking Portal sees it: what a clinic can search, the Providers it
    /// contracts, and the ones it can give trips to (CATALOG_MODEL.md).
    /// </summary>
    public interface IPortalCatalogService
    {
        Task<List<PortalCatalogCategoryDto>> GetCategoriesAsync(CancellationToken ct);

        Task<PortalCatalogPageDto<PortalCatalogProviderRowDto>> SearchProvidersAsync(PortalCatalogSearchDto request, CancellationToken ct);

        Task<PortalCatalogProviderDetailDto?> GetProviderAsync(int id, CancellationToken ct);

        Task ContractAsync(int catalogProviderId, CancellationToken ct);

        Task UncontractAsync(int catalogProviderId, CancellationToken ct);

        Task<PortalCatalogProviderDetailDto> EditProviderAsync(int id, PortalCatalogProviderEditDto request, CancellationToken ct);

        Task<List<AssignableProviderDto>> GetAssignableProvidersAsync(CancellationToken ct);

        /// <summary>Whether a trip of this clinic may be given to this Provider account.</summary>
        Task<bool> IsAssignableAsync(int providerId, CancellationToken ct);
    }

    public sealed class PortalCatalogService : IPortalCatalogService
    {
        private const int MaxPageSize = 100;
        private const int MaxCityFacets = 50;

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ICatalogAccountSync _sync;
        private readonly IRoutingService _routing;

        public PortalCatalogService(
            RaphaelContext context,
            ICurrentUserService currentUser,
            ICatalogAccountSync sync,
            IRoutingService routing)
        {
            _context = context;
            _currentUser = currentUser;
            _sync = sync;
            _routing = routing;
        }

        /// <summary>The caller's clinic. Every method needs one: this is the clinic's view of the catalog.</summary>
        private int ClinicId => _currentUser.IntegratorId ?? throw new PortalCatalogDeniedException(notFound: true);

        /// <summary>A clinic's admin: role 1 and an integrator (user's definition, 2026-10-07).</summary>
        private bool IsClinicAdmin => _currentUser.IntegratorId != null && _currentUser.RoleId == 1;

        private IQueryable<int> ContractedIds(int clinicId) =>
            _context.IntegratorProviders.Where(r => r.IntegratorId == clinicId).Select(r => r.CatalogProviderId);

        public async Task<List<PortalCatalogCategoryDto>> GetCategoriesAsync(CancellationToken ct)
        {
            _ = ClinicId;

            var groups = await _context.CatalogProviderCategories
                .AsNoTracking()
                .Where(g => g.IsActive)
                .OrderBy(g => g.DisplayOrder)
                .Select(g => new CatalogCategoryDto { Id = g.Id, Code = g.Code, NameEn = g.NameEn, NameEs = g.NameEs, DisplayOrder = g.DisplayOrder })
                .ToListAsync(ct);

            // The only category a clinic sees today. A new one is one more entry here.
            return new List<PortalCatalogCategoryDto>
            {
                new() { Key = "providers", NameEn = "Providers", NameEs = "Proveedores", Groups = groups }
            };
        }

        public async Task<PortalCatalogPageDto<PortalCatalogProviderRowDto>> SearchProvidersAsync(PortalCatalogSearchDto request, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var contracted = ContractedIds(clinicId);

            // Active entries, plus the inactive ones this clinic still has contracted: it must see
            // that they are no longer available rather than have them vanish from its list.
            var query = _context.CatalogProviders.AsNoTracking()
                .Where(c => c.IsActive || contracted.Contains(c.Id));

            foreach (var term in CatalogText.SplitTerms(request.Term))
            {
                var needle = term;
                query = CatalogText.LooksLikePhone(needle)
                    ? query.Where(c => c.SearchText.Contains(needle) || (c.PhoneDigits != null && c.PhoneDigits.Contains(needle)))
                    : query.Where(c => c.SearchText.Contains(needle));
            }

            if (request.Contracted == true) query = query.Where(c => contracted.Contains(c.Id));
            if (request.Contracted == false) query = query.Where(c => !contracted.Contains(c.Id));

            // Facets are counted before their own filter, so choosing a group still shows the others.
            var byPlace = query;
            if (request.GroupId is int groupId) byPlace = byPlace.Where(c => c.CategoryId == groupId);

            var groupFacets = await query
                .GroupBy(c => new { c.CategoryId, c.Category.NameEn })
                .Select(g => new CatalogFacetValueDto { Id = g.Key.CategoryId, Label = g.Key.NameEn, Count = g.Count() })
                .ToListAsync(ct);

            var countyFacets = await byPlace
                .Where(c => c.CountyId != null)
                .GroupBy(c => new { c.CountyId, c.County!.Name })
                .Select(g => new CatalogFacetValueDto { Id = g.Key.CountyId, Label = g.Key.Name, Count = g.Count() })
                .OrderBy(f => f.Label)
                .ToListAsync(ct);

            var filtered = byPlace;
            if (request.CountyId is int countyId) filtered = filtered.Where(c => c.CountyId == countyId);

            var cityFacets = await filtered
                .Where(c => c.City != null)
                .GroupBy(c => c.City!)
                .Select(g => new CatalogFacetValueDto { Label = g.Key, Count = g.Count() })
                .OrderByDescending(f => f.Count).ThenBy(f => f.Label)
                .Take(MaxCityFacets)
                .ToListAsync(ct);

            if (!string.IsNullOrWhiteSpace(request.City)) filtered = filtered.Where(c => c.City == request.City.Trim());

            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
            var pageNumber = Math.Max(1, request.PageNumber);
            var total = await filtered.CountAsync(ct);
            var contractedCount = await filtered.CountAsync(c => contracted.Contains(c.Id), ct);

            var items = await filtered
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new PortalCatalogProviderRowDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    GroupId = c.CategoryId,
                    GroupNameEn = c.Category.NameEn,
                    GroupNameEs = c.Category.NameEs,
                    City = c.City,
                    County = c.County != null ? c.County.Name : c.CountyRaw,
                    State = c.State,
                    Phone = c.Phone,
                    Email = c.Email,
                    Website = c.Website,
                    IsActive = c.IsActive,
                    Contracted = contracted.Contains(c.Id),
                    OperatesInRaphael = _context.Providers.Any(p => p.CatalogProviderId == c.Id)
                })
                .ToListAsync(ct);

            return new PortalCatalogPageDto<PortalCatalogProviderRowDto>
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Groups = groupFacets.OrderBy(f => f.Label).ToList(),
                Counties = countyFacets,
                Cities = cityFacets,
                ContractedCount = contractedCount
            };
        }

        public async Task<PortalCatalogProviderDetailDto?> GetProviderAsync(int id, CancellationToken ct)
        {
            var clinicId = ClinicId;
            var contracted = ContractedIds(clinicId);

            var detail = await _context.CatalogProviders.AsNoTracking()
                .Where(c => c.Id == id && (c.IsActive || contracted.Contains(c.Id)))
                .Select(c => new PortalCatalogProviderDetailDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    GroupId = c.CategoryId,
                    GroupNameEn = c.Category.NameEn,
                    GroupNameEs = c.Category.NameEs,
                    Address = c.Address,
                    City = c.City,
                    County = c.County != null ? c.County.Name : c.CountyRaw,
                    State = c.State,
                    Zip = c.Zip,
                    Phone = c.Phone,
                    Email = c.Email,
                    Website = c.Website,
                    ContactName = c.ContactName,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    Npi = c.Npi,
                    ServiceLevel = c.ServiceLevel,
                    CoverageArea = c.CoverageArea,
                    EmsLicense = c.EmsLicense,
                    LicenseExpiresOn = c.LicenseExpiresOn,
                    PlanSegment = c.PlanSegment,
                    IsActive = c.IsActive,
                    Contracted = contracted.Contains(c.Id),
                    OperatesInRaphael = _context.Providers.Any(p => p.CatalogProviderId == c.Id)
                })
                .FirstOrDefaultAsync(ct);

            if (detail is null) return null;

            detail.CanContract = IsClinicAdmin;
            detail.CanEdit = IsClinicAdmin && detail.Contracted;
            return detail;
        }

        public async Task ContractAsync(int catalogProviderId, CancellationToken ct)
        {
            var clinicId = ClinicId;
            if (!IsClinicAdmin) throw new PortalCatalogDeniedException(notFound: false);

            var row = await _context.CatalogProviders.AsNoTracking()
                .Where(c => c.Id == catalogProviderId)
                .Select(c => new { c.IsActive })
                .FirstOrDefaultAsync(ct) ?? throw new PortalCatalogDeniedException(notFound: true);

            if (!row.IsActive) throw new PortalCatalogRuleException("This provider is no longer active in the catalog and cannot be contracted.");

            if (await _context.IntegratorProviders.AnyAsync(r => r.IntegratorId == clinicId && r.CatalogProviderId == catalogProviderId, ct))
            {
                return; // Already contracted: asking twice is not an error.
            }

            _context.IntegratorProviders.Add(new IntegratorProvider
            {
                IntegratorId = clinicId,
                CatalogProviderId = catalogProviderId,
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = _currentUser.UserId
            });
            await _context.SaveChangesAsync(ct);
        }

        public async Task UncontractAsync(int catalogProviderId, CancellationToken ct)
        {
            var clinicId = ClinicId;
            if (!IsClinicAdmin) throw new PortalCatalogDeniedException(notFound: false);

            // Trips already given to this Provider keep it: removing it from the list only stops new ones.
            var link = await _context.IntegratorProviders
                .FirstOrDefaultAsync(r => r.IntegratorId == clinicId && r.CatalogProviderId == catalogProviderId, ct);
            if (link is null) return;

            _context.IntegratorProviders.Remove(link);
            await _context.SaveChangesAsync(ct);
        }

        public async Task<PortalCatalogProviderDetailDto> EditProviderAsync(int id, PortalCatalogProviderEditDto request, CancellationToken ct)
        {
            var clinicId = ClinicId;
            if (!IsClinicAdmin) throw new PortalCatalogDeniedException(notFound: false);
            if (!await ContractedIds(clinicId).AnyAsync(x => x == id, ct)) throw new PortalCatalogDeniedException(notFound: true);
            if (string.IsNullOrWhiteSpace(request.Name)) throw new PortalCatalogRuleException("The name is required.");

            var row = await _context.CatalogProviders.FirstAsync(c => c.Id == id, ct);
            var placeChanged = !Same(row.Address, request.Address) || !Same(row.City, request.City)
                               || !Same(row.State, request.State) || !Same(row.Zip, request.Zip);

            row.Name = request.Name.Trim();
            row.Address = Clean(request.Address);
            row.City = Clean(request.City);
            row.State = Clean(request.State);
            row.Zip = Clean(request.Zip);
            row.Phone = Clean(request.Phone);
            row.Email = Clean(request.Email);
            row.Website = Clean(request.Website);
            row.ContactName = Clean(request.ContactName);

            if (placeChanged)
            {
                // A new place needs new coordinates. Resolved through the backend's geocode cache:
                // paid only for an address nobody has resolved before.
                var line = RouteCacheKey.ComposeAddress(row.Address, row.City, row.State, row.Zip);
                var found = string.IsNullOrWhiteSpace(line)
                    ? null
                    : await _routing.GeocodeAsync(new GeocodeRequestDto { Address = line }, ct);
                row.Latitude = found?.Latitude;
                row.Longitude = found?.Longitude;
                row.GeocodeStatus = found?.Latitude != null ? GeocodeStatus.Resolved : GeocodeStatus.NotFound;
                row.GeocodedAtUtc = DateTime.UtcNow;
            }

            row.PhoneDigits = CatalogText.DigitsOnly(row.Phone);
            row.MatchKey = CatalogText.BuildMatchKey(row.CategoryId, row.Name, row.Zip);
            row.SearchText = CatalogText.BuildSearchText(row.Name, row.City, row.CountyRaw, row.Zip, row.Address, row.ContactName);

            if (await _context.CatalogProviders.AnyAsync(c => c.Id != row.Id && c.MatchKey == row.MatchKey, ct))
            {
                throw new CatalogConflictException();
            }

            row.UpdatedAtUtc = DateTime.UtcNow;
            row.UpdatedByUserId = _currentUser.UserId;
            row.UpdatedByProviderId = _currentUser.ProviderId;

            // Every account made out of this entry takes the change: one version of the Provider.
            await _sync.SpreadAsync(row, ct);
            await _context.SaveChangesAsync(ct);

            return (await GetProviderAsync(id, ct))!;
        }

        public async Task<List<AssignableProviderDto>> GetAssignableProvidersAsync(CancellationToken ct)
        {
            var clinicId = ClinicId;
            return await AssignableQuery(clinicId)
                .OrderBy(p => p.Name)
                .Select(p => new AssignableProviderDto { ProviderId = p.Id, Name = p.Name })
                .ToListAsync(ct);
        }

        public Task<bool> IsAssignableAsync(int providerId, CancellationToken ct) =>
            AssignableQuery(ClinicId).AnyAsync(p => p.Id == providerId, ct);

        /// <summary>Accounts whose catalog entry this clinic contracted and which is still active.</summary>
        private IQueryable<Provider> AssignableQuery(int clinicId)
        {
            var contracted = ContractedIds(clinicId);
            return _context.Providers.AsNoTracking()
                .Where(p => p.CatalogProviderId != null
                            && contracted.Contains(p.CatalogProviderId.Value)
                            && p.CatalogProvider!.IsActive);
        }

        private static bool Same(string? a, string? b) =>
            string.Equals(Clean(a), Clean(b), StringComparison.OrdinalIgnoreCase);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
