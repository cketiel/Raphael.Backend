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

        Task<List<PortalProviderUserDto>> GetProviderUsersAsync(int catalogProviderId, CancellationToken ct);

        Task<PortalProviderUserDto> CreateProviderUserAsync(int catalogProviderId, PortalProviderUserCreateDto dto, CancellationToken ct);

        Task<PortalProviderUserDto> SetProviderUserActiveAsync(int catalogProviderId, int userId, bool isActive, CancellationToken ct);
    }

    public sealed class PortalCatalogService : IPortalCatalogService
    {
        /// <summary>The only role a clinic gives an external Provider's user: User (decided 2026-10-11).</summary>
        private const int ProviderUserRoleId = 3;

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
            detail.CanEdit = IsClinicAdmin;

            var account = await AccountOf(id)
                .Select(p => new { p.Id, p.WorkMode, ActiveUsers = _context.Users.Count(u => u.ProviderId == p.Id && u.IsActive) })
                .FirstOrDefaultAsync(ct);
            detail.AccountId = account?.Id;
            detail.WorkMode = account?.WorkMode.ToString();
            detail.ActiveUserCount = account?.ActiveUsers ?? 0;
            detail.Assignable = detail.Contracted && detail.IsActive && detail.ActiveUserCount > 0;
            // A Provider that works in Raphael has its users managed by the office.
            detail.CanManageUsers = IsClinicAdmin && detail.Contracted && account?.WorkMode != ProviderWorkMode.Raphael;
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

            // The Provider joins the official list as it is contracted, so it can have users and
            // trips (PROVIDER_ACCESS_MODEL.md §3.3). Also on a second request: a contract made before
            // this rule gets its account the same way.
            await EnsureAccountAsync(catalogProviderId, ct);

            if (await _context.IntegratorProviders.AnyAsync(r => r.IntegratorId == clinicId && r.CatalogProviderId == catalogProviderId, ct))
            {
                await _context.SaveChangesAsync(ct);
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
            // Any entry the clinic can see, contracted or not (decision C, revised 2026-10-08): the
            // catalog is shared, so the edit is authored and every app sees it.
            var contracted = ContractedIds(clinicId);
            if (!await _context.CatalogProviders.AnyAsync(c => c.Id == id && (c.IsActive || contracted.Contains(c.Id)), ct))
                throw new PortalCatalogDeniedException(notFound: true);
            if (string.IsNullOrWhiteSpace(request.Name)) throw new PortalCatalogRuleException("The name is required.");

            var row = await _context.CatalogProviders.FirstAsync(c => c.Id == id, ct);

            // ⚠️ Everything is worked out before the row is touched. Geocoding writes to its cache
            // with SaveChanges on this same context, and that save would carry a half-applied row
            // with it: a rename refused with 409 was stored anyway (found testing, 2026-10-07).
            var name = request.Name.Trim();
            var address = Clean(request.Address);
            var city = Clean(request.City);
            var state = Clean(request.State);
            var zip = Clean(request.Zip);

            var matchKey = CatalogText.BuildMatchKey(row.CategoryId, name, zip);
            if (await _context.CatalogProviders.AnyAsync(c => c.Id != row.Id && c.MatchKey == matchKey, ct))
            {
                throw new CatalogConflictException();
            }

            var placeChanged = !Same(row.Address, address) || !Same(row.City, city)
                               || !Same(row.State, state) || !Same(row.Zip, zip);
            GeocodeResultDto? found = null;
            if (placeChanged)
            {
                // A new place needs new coordinates. Resolved through the backend's geocode cache:
                // paid only for an address nobody has resolved before.
                var line = RouteCacheKey.ComposeAddress(address, city, state, zip);
                found = string.IsNullOrWhiteSpace(line)
                    ? null
                    : await _routing.GeocodeAsync(new GeocodeRequestDto { Address = line }, ct);
            }

            row.Name = name;
            row.Address = address;
            row.City = city;
            row.State = state;
            row.Zip = zip;
            row.Phone = Clean(request.Phone);
            row.Email = Clean(request.Email);
            row.Website = Clean(request.Website);
            row.ContactName = Clean(request.ContactName);

            if (placeChanged)
            {
                row.Latitude = found?.Latitude;
                row.Longitude = found?.Longitude;
                row.GeocodeStatus = found?.Latitude != null ? GeocodeStatus.Resolved : GeocodeStatus.NotFound;
                row.GeocodedAtUtc = DateTime.UtcNow;
            }

            row.PhoneDigits = CatalogText.DigitsOnly(row.Phone);
            row.MatchKey = matchKey;
            row.SearchText = CatalogText.BuildSearchText(row.Name, row.City, row.CountyRaw, row.Zip, row.Address, row.ContactName);

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

        /// <summary>
        /// Accounts whose catalog entry this clinic contracted, still active, and with at least one
        /// active user: a trip given to an account nobody can see leaves the patient without a ride
        /// (PROVIDER_ACCESS_MODEL.md §3.3).
        /// </summary>
        private IQueryable<Provider> AssignableQuery(int clinicId)
        {
            var contracted = ContractedIds(clinicId);
            return _context.Providers.AsNoTracking()
                .Where(p => p.CatalogProviderId != null
                            && contracted.Contains(p.CatalogProviderId.Value)
                            && p.CatalogProvider!.IsActive
                            && _context.Users.Any(u => u.ProviderId == p.Id && u.IsActive));
        }

        /// <summary>The account made out of a catalog entry. One per real company: the oldest if the Desktop made more.</summary>
        private IQueryable<Provider> AccountOf(int catalogProviderId) =>
            _context.Providers.Where(p => p.CatalogProviderId == catalogProviderId).OrderBy(p => p.Id);

        /// <summary>
        /// The account of a catalog entry, created when it has none: an external Provider (it works
        /// in another system until it moves to the Desktop), with no users and no owner company.
        /// Added to the context, saved with the caller's changes.
        /// </summary>
        private Task<Provider> EnsureAccountAsync(int catalogProviderId, CancellationToken ct) =>
            _sync.EnsureProviderAccountAsync(catalogProviderId, ct);

        // ------------------------------------------------------------------ an external Provider's users

        /// <summary>
        /// The account of a Provider this clinic contracted, for its admin to manage the users of.
        /// A Provider that works in Raphael has its users managed by the office.
        /// </summary>
        private async Task<Provider?> ManagedAccountAsync(int catalogProviderId, bool create, CancellationToken ct)
        {
            var clinicId = ClinicId;
            if (!IsClinicAdmin) throw new PortalCatalogDeniedException(notFound: false);

            if (!await ContractedIds(clinicId).AnyAsync(id => id == catalogProviderId, ct))
                throw new PortalCatalogDeniedException(notFound: true);

            var account = create ? await EnsureAccountAsync(catalogProviderId, ct) : await AccountOf(catalogProviderId).FirstOrDefaultAsync(ct);
            if (account?.WorkMode == ProviderWorkMode.Raphael)
                throw new PortalCatalogRuleException("This Provider works in Raphael: its users are managed by the office.");
            return account;
        }

        private static System.Linq.Expressions.Expression<Func<User, PortalProviderUserDto>> ToProviderUserDto() => u => new PortalProviderUserDto
        {
            Id = u.Id,
            FullName = u.FullName,
            Username = u.Username,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            MustChangePassword = u.MustChangePassword
        };

        public async Task<List<PortalProviderUserDto>> GetProviderUsersAsync(int catalogProviderId, CancellationToken ct)
        {
            var account = await ManagedAccountAsync(catalogProviderId, create: false, ct);
            if (account is null) return new List<PortalProviderUserDto>();

            return await _context.Users.AsNoTracking()
                .Where(u => u.ProviderId == account.Id)
                .OrderBy(u => u.FullName)
                .Select(ToProviderUserDto())
                .ToListAsync(ct);
        }

        /// <remarks>
        /// The user sees the trips every clinic gives this Provider, not only this clinic's (decided
        /// 2026-10-11). That is why it is born unable to do anything but change its password: the
        /// clinic that typed the first one must not keep a way in (PasswordChangeGate).
        /// </remarks>
        public async Task<PortalProviderUserDto> CreateProviderUserAsync(int catalogProviderId, PortalProviderUserCreateDto dto, CancellationToken ct)
        {
            var name = dto.FullName?.Trim() ?? "";
            var login = dto.Username?.Trim() ?? "";
            if (name.Length == 0) throw new PortalCatalogRuleException("The full name is required.");
            if (login.Length == 0) throw new PortalCatalogRuleException("The username is required.");
            if (Raphael.Api.Services.Auth.PasswordRules.Problem(dto.Password) is string problem) throw new PortalCatalogRuleException(problem);

            // Usernames are unique across the whole system: sign-in knows no company.
            if (await _context.Users.AnyAsync(u => u.Username == login, ct))
                throw new PortalCatalogRuleException("That username is already taken.");

            var account = (await ManagedAccountAsync(catalogProviderId, create: true, ct))!;

            var user = new User
            {
                FullName = name,
                Username = login,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
                RoleId = ProviderUserRoleId,
                IsActive = true,
                MustChangePassword = true,
                Provider = account,
                IntegratorId = null
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync(ct);

            return await _context.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(ToProviderUserDto()).FirstAsync(ct);
        }

        /// <remarks>Switching a user off ends every open session of it.</remarks>
        public async Task<PortalProviderUserDto> SetProviderUserActiveAsync(int catalogProviderId, int userId, bool isActive, CancellationToken ct)
        {
            var account = await ManagedAccountAsync(catalogProviderId, create: false, ct)
                ?? throw new PortalCatalogDeniedException(notFound: true);

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId && u.ProviderId == account.Id, ct)
                ?? throw new PortalCatalogDeniedException(notFound: true);

            user.IsActive = isActive;
            if (!isActive)
            {
                var now = DateTime.UtcNow;
                foreach (var token in await _context.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAtUtc == null).ToListAsync(ct))
                {
                    token.RevokedAtUtc = now;
                    token.RevokedReason = "disabled-by-clinic";
                }
            }
            await _context.SaveChangesAsync(ct);

            return await _context.Users.AsNoTracking().Where(u => u.Id == user.Id).Select(ToProviderUserDto()).FirstAsync(ct);
        }

        private static bool Same(string? a, string? b) =>
            string.Equals(Clean(a), Clean(b), StringComparison.OrdinalIgnoreCase);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
