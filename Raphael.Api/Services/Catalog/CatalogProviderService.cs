using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Routing;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs;
using Raphael.Shared.DTOs.Catalog;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Helpers;
using Raphael.Shared.Interfaces;

namespace Raphael.Api.Services.Catalog
{
    /// <inheritdoc cref="ICatalogProviderService"/>
    public sealed class CatalogProviderService : ICatalogProviderService
    {
        private const int MaxPageSize = 200;
        private const int MaxSuggestions = 5;
        private const int MaxFacetValues = 60;

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IRoutingService _routing;
        private readonly ILogger<CatalogProviderService> _logger;

        public CatalogProviderService(
            RaphaelContext context,
            ICurrentUserService currentUser,
            IRoutingService routing,
            ILogger<CatalogProviderService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _routing = routing;
            _logger = logger;
        }

        /// <summary>
        /// The rows of <c>Providers</c> that belong to whoever is asking.
        /// </summary>
        /// <remarks>
        /// ⚠️ The branch matters. <c>OwnerProviderId == providerId</c> with a null parameter
        /// becomes <c>= NULL</c> in SQL, which is never true, and the general broker would see
        /// none of its own rows as its own. See the twin in CatalogIntegratorService.
        /// </remarks>
        private IQueryable<Provider> MyProviders()
        {
            var providerId = _currentUser.ProviderId;

            return providerId.HasValue
                ? _context.Providers.Where(p => p.OwnerProviderId == providerId.Value)
                : _context.Providers.Where(p => p.OwnerProviderId == null);
        }

        private bool CallerIsBroker => _currentUser.ProviderId is null;

        public async Task<CatalogPageDto<CatalogProviderListItemDto>> SearchAsync(
            CatalogSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
            var pageNumber = Math.Max(request.PageNumber, 1);

            var mine = MyProviders();
            var query = _context.CatalogProviders.AsNoTracking().AsQueryable();

            if (!request.IncludeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            query = ApplyFilters(query, request, mine);

            var terms = CatalogText.SplitTerms(request.Term);
            query = ApplyTerms(query, terms);

            var totalCount = await query.CountAsync(cancellationToken);

            var ordered = ApplySort(query, request.SortBy, CatalogText.Normalize(request.Term));

            var items = await ordered
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new CatalogProviderListItemDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    CategoryId = c.CategoryId,
                    CategoryCode = c.Category.Code,
                    City = c.City,
                    CountyName = c.County != null ? c.County.Name : c.CountyRaw,
                    State = c.State,
                    Zip = c.Zip,
                    Phone = c.Phone,
                    Email = c.Email,
                    Website = c.Website,
                    ContactName = c.ContactName,
                    Npi = c.Npi,
                    ServiceLevel = c.ServiceLevel,
                    LicenseExpiresOn = c.LicenseExpiresOn,
                    IsActive = c.IsActive,
                    HasCoordinates = c.Latitude != null && c.Longitude != null,
                    MyProviderId = mine
                        .Where(p => p.CatalogProviderId == c.Id)
                        .Select(p => (int?)p.Id)
                        .FirstOrDefault(),
                    AddedByCompanyCount = _context.Providers
                        .Count(p => p.CatalogProviderId == c.Id)
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.InMyCompany = item.MyProviderId.HasValue;

                if (!CallerIsBroker)
                {
                    item.AddedByCompanyCount = null;
                }
            }

            var page = new CatalogPageDto<CatalogProviderListItemDto>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            if (request.IncludeFacets)
            {
                page.Facets = await BuildFacetsAsync(request, mine, terms, cancellationToken);
            }

            if (totalCount == 0 && terms.Count > 0)
            {
                page.Suggestions = await SuggestAsync(terms, request.IncludeInactive, cancellationToken);
            }

            return page;
        }

        private IQueryable<CatalogProvider> ApplyFilters(
            IQueryable<CatalogProvider> query,
            CatalogSearchRequestDto request,
            IQueryable<Provider> mine)
        {
            if (request.CategoryId.HasValue)
            {
                query = query.Where(c => c.CategoryId == request.CategoryId.Value);
            }

            if (request.CountyId.HasValue)
            {
                query = query.Where(c => c.CountyId == request.CountyId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.City))
            {
                var city = request.City.Trim();
                query = query.Where(c => c.City == city);
            }

            if (!string.IsNullOrWhiteSpace(request.State))
            {
                var state = request.State.Trim();
                query = query.Where(c => c.State == state);
            }

            if (request.InMyCompany == true)
            {
                query = query.Where(c => mine.Any(p => p.CatalogProviderId == c.Id));
            }
            else if (request.InMyCompany == false)
            {
                query = query.Where(c => !mine.Any(p => p.CatalogProviderId == c.Id));
            }

            if (request.HasPhone.HasValue)
            {
                query = request.HasPhone.Value
                    ? query.Where(c => c.Phone != null && c.Phone != "")
                    : query.Where(c => c.Phone == null || c.Phone == "");
            }

            if (request.HasEmail.HasValue)
            {
                query = request.HasEmail.Value
                    ? query.Where(c => c.Email != null && c.Email != "")
                    : query.Where(c => c.Email == null || c.Email == "");
            }

            if (request.HasWebsite.HasValue)
            {
                query = request.HasWebsite.Value
                    ? query.Where(c => c.Website != null && c.Website != "")
                    : query.Where(c => c.Website == null || c.Website == "");
            }

            return query;
        }

        private static IQueryable<CatalogProvider> ApplyTerms(
            IQueryable<CatalogProvider> query,
            IReadOnlyList<string> terms)
        {
            foreach (var term in terms)
            {
                var needle = term;

                // An NPI is ten digits, so a digits-only term is tried against it too: pasting
                // an NPI from a spreadsheet is how these rows get looked up in practice.
                query = CatalogText.LooksLikePhone(needle)
                    ? query.Where(c => c.SearchText.Contains(needle)
                        || (c.PhoneDigits != null && c.PhoneDigits.Contains(needle))
                        || (c.Npi != null && c.Npi.Contains(needle)))
                    : query.Where(c => c.SearchText.Contains(needle));
            }

            return query;
        }

        private static IQueryable<CatalogProvider> ApplySort(
            IQueryable<CatalogProvider> query,
            CatalogSortBy sortBy,
            string normalizedTerm)
        {
            switch (sortBy)
            {
                case CatalogSortBy.Name:
                    return query.OrderBy(c => c.Name).ThenBy(c => c.Id);

                case CatalogSortBy.City:
                    return query.OrderBy(c => c.City).ThenBy(c => c.Name).ThenBy(c => c.Id);

                case CatalogSortBy.Newest:
                    return query.OrderByDescending(c => c.CreatedAtUtc).ThenBy(c => c.Id);

                default:
                    if (normalizedTerm.Length == 0)
                    {
                        return query.OrderBy(c => c.Name).ThenBy(c => c.Id);
                    }

                    var wordStart = " " + normalizedTerm;

                    return query
                        .OrderByDescending(c => c.SearchText.StartsWith(normalizedTerm)
                            ? 3
                            : c.SearchText.Contains(wordStart) ? 2 : 1)
                        .ThenBy(c => c.Name)
                        .ThenBy(c => c.Id);
            }
        }

        private async Task<CatalogFacetsDto> BuildFacetsAsync(
            CatalogSearchRequestDto request,
            IQueryable<Provider> mine,
            IReadOnlyList<string> terms,
            CancellationToken cancellationToken)
        {
            var baseQuery = _context.CatalogProviders.AsNoTracking().AsQueryable();

            if (!request.IncludeInactive)
            {
                baseQuery = baseQuery.Where(c => c.IsActive);
            }

            baseQuery = ApplyTerms(baseQuery, terms);

            var withoutCategory = ApplyFilters(baseQuery, CloneWithout(request, clearCategory: true), mine);
            var withoutCounty = ApplyFilters(baseQuery, CloneWithout(request, clearCounty: true), mine);
            var withoutCity = ApplyFilters(baseQuery, CloneWithout(request, clearCity: true), mine);
            var withoutOwnership = ApplyFilters(baseQuery, CloneWithout(request, clearOwnership: true), mine);

            var categories = await withoutCategory
                .GroupBy(c => new { c.CategoryId, c.Category.Code })
                .Select(g => new CatalogFacetValueDto
                {
                    Id = g.Key.CategoryId,
                    Label = g.Key.Code,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(MaxFacetValues)
                .ToListAsync(cancellationToken);

            var counties = await withoutCounty
                .Where(c => c.CountyId != null)
                .GroupBy(c => new { c.CountyId, c.County!.Name })
                .Select(g => new CatalogFacetValueDto
                {
                    Id = g.Key.CountyId,
                    Label = g.Key.Name,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(MaxFacetValues)
                .ToListAsync(cancellationToken);

            var cities = await withoutCity
                .Where(c => c.City != null && c.City != "")
                .GroupBy(c => c.City!)
                .Select(g => new CatalogFacetValueDto
                {
                    Label = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(f => f.Count)
                .Take(MaxFacetValues)
                .ToListAsync(cancellationToken);

            var inMyCompany = await withoutOwnership
                .CountAsync(c => mine.Any(p => p.CatalogProviderId == c.Id), cancellationToken);

            var total = await withoutOwnership.CountAsync(cancellationToken);

            return new CatalogFacetsDto
            {
                Categories = categories,
                Counties = counties,
                Cities = cities,
                InMyCompanyCount = inMyCompany,
                NotInMyCompanyCount = total - inMyCompany
            };
        }

        private static CatalogSearchRequestDto CloneWithout(
            CatalogSearchRequestDto source,
            bool clearCategory = false,
            bool clearCounty = false,
            bool clearCity = false,
            bool clearOwnership = false)
        {
            return new CatalogSearchRequestDto
            {
                CategoryId = clearCategory ? null : source.CategoryId,
                CountyId = clearCounty ? null : source.CountyId,
                City = clearCity ? null : source.City,
                State = source.State,
                InMyCompany = clearOwnership ? null : source.InMyCompany,
                HasPhone = source.HasPhone,
                HasEmail = source.HasEmail,
                HasWebsite = source.HasWebsite
            };
        }

        /// <summary>
        /// When nothing matched, offer names that start the way the longest term does.
        /// See the twin in CatalogIntegratorService for why a prefix and not DIFFERENCE.
        /// </summary>
        private async Task<IReadOnlyList<CatalogSuggestionDto>> SuggestAsync(
            IReadOnlyList<string> terms,
            bool includeInactive,
            CancellationToken cancellationToken)
        {
            var longest = terms.OrderByDescending(t => t.Length).First();

            if (longest.Length < 4)
            {
                return Array.Empty<CatalogSuggestionDto>();
            }

            var prefix = longest[..Math.Min(4, longest.Length)];

            var query = _context.CatalogProviders.AsNoTracking().AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(c => c.IsActive);
            }

            return await query
                .Where(c => c.SearchText.Contains(prefix))
                .OrderBy(c => c.Name)
                .Take(MaxSuggestions)
                .Select(c => new CatalogSuggestionDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    City = c.City
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<CatalogProviderDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var row = await _context.CatalogProviders
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.County)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            return row is null ? null : await ToDetailAsync(row, cancellationToken);
        }

        public async Task<CatalogProviderDto> CreateAsync(
            CatalogProviderSaveDto request,
            CancellationToken cancellationToken)
        {
            var row = new CatalogProvider
            {
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = _currentUser.UserId,
                CreatedByProviderId = _currentUser.ProviderId
            };

            Apply(request, row);

            _context.CatalogProviders.Add(row);
            await _context.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(row.Id, cancellationToken))!;
        }

        public async Task<CatalogProviderDto?> UpdateAsync(
            int id,
            CatalogProviderSaveDto request,
            CancellationToken cancellationToken)
        {
            var row = await _context.CatalogProviders
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (row is null)
            {
                return null;
            }

            Apply(request, row);

            row.UpdatedAtUtc = DateTime.UtcNow;
            row.UpdatedByUserId = _currentUser.UserId;
            row.UpdatedByProviderId = _currentUser.ProviderId;

            await _context.SaveChangesAsync(cancellationToken);

            return await GetByIdAsync(id, cancellationToken);
        }

        /// <summary>
        /// Copies the editable fields and rebuilds everything derived from them, in one place
        /// so a new phone can never sit next to a stale <c>PhoneDigits</c>.
        /// </summary>
        private static void Apply(CatalogProviderSaveDto request, CatalogProvider row)
        {
            row.CategoryId = request.CategoryId;
            row.Name = request.Name.Trim();
            row.Address = Clean(request.Address);
            row.City = Clean(request.City);
            row.CountyId = request.CountyId;
            row.State = Clean(request.State);
            row.Zip = Clean(request.Zip);
            row.Phone = Clean(request.Phone);
            row.Email = Clean(request.Email);
            row.Website = Clean(request.Website);
            row.ContactName = Clean(request.ContactName);
            row.Npi = Clean(request.Npi);
            row.IsPrimaryNemt = request.IsPrimaryNemt;
            row.SourceUpdatedOn = request.SourceUpdatedOn;
            row.EmsLicense = Clean(request.EmsLicense);
            row.ServiceLevel = Clean(request.ServiceLevel);
            row.LicenseExpiresOn = request.LicenseExpiresOn;
            row.PlanSegment = Clean(request.PlanSegment);
            row.CoverageArea = Clean(request.CoverageArea);
            row.ProviderContact = Clean(request.ProviderContact);
            row.EvidenceNote = Clean(request.EvidenceNote);
            row.Comments = request.Comments;
            row.IsActive = request.IsActive;

            row.PhoneDigits = CatalogText.DigitsOnly(row.Phone);
            row.MatchKey = CatalogText.BuildMatchKey(row.CategoryId, row.Name, row.Zip);
            row.SearchText = CatalogText.BuildSearchText(
                row.Name, row.City, row.CountyRaw, row.Zip, row.Address, row.ContactName);
        }

        private static string? Clean(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            return value.Trim();
        }

        private async Task<CatalogProviderDto> ToDetailAsync(
            CatalogProvider row,
            CancellationToken cancellationToken)
        {
            var myId = await MyProviders()
                .Where(p => p.CatalogProviderId == row.Id)
                .Select(p => (int?)p.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var addedBy = CallerIsBroker
                ? await _context.Providers
                    .CountAsync(p => p.CatalogProviderId == row.Id, cancellationToken)
                : (int?)null;

            var userIds = new[] { row.CreatedByUserId, row.UpdatedByUserId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

            var providerIds = new[] { row.CreatedByProviderId, row.UpdatedByProviderId }
                .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

            var userNames = userIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Users
                    .AsNoTracking()
                    .Where(u => userIds.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

            var providerNames = providerIds.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Providers
                    .AsNoTracking()
                    .Where(p => providerIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            string? UserName(int? id) =>
                id.HasValue && userNames.TryGetValue(id.Value, out var name) ? name : null;

            string? ProviderName(int? id) =>
                id.HasValue && providerNames.TryGetValue(id.Value, out var name) ? name : null;

            return new CatalogProviderDto
            {
                Id = row.Id,
                CategoryId = row.CategoryId,
                CategoryCode = row.Category?.Code,
                Name = row.Name,
                Address = row.Address,
                City = row.City,
                CountyId = row.CountyId,
                CountyName = row.County?.Name,
                CountyRaw = row.CountyRaw,
                State = row.State,
                Zip = row.Zip,
                Phone = row.Phone,
                Email = row.Email,
                Website = row.Website,
                ContactName = row.ContactName,
                Npi = row.Npi,
                IsPrimaryNemt = row.IsPrimaryNemt,
                SourceUpdatedOn = row.SourceUpdatedOn,
                EmsLicense = row.EmsLicense,
                ServiceLevel = row.ServiceLevel,
                LicenseExpiresOn = row.LicenseExpiresOn,
                PlanSegment = row.PlanSegment,
                CoverageArea = row.CoverageArea,
                ProviderContact = row.ProviderContact,
                EvidenceNote = row.EvidenceNote,
                Comments = row.Comments,
                IsActive = row.IsActive,
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                GeocodeStatus = (int)row.GeocodeStatus,
                GeocodedAtUtc = row.GeocodedAtUtc,
                SourceFile = row.SourceFile,
                SourceRow = row.SourceRow,
                CreatedAtUtc = row.CreatedAtUtc,
                CreatedByUserId = row.CreatedByUserId,
                CreatedByUserName = UserName(row.CreatedByUserId),
                CreatedByProviderId = row.CreatedByProviderId,
                CreatedByProviderName = ProviderName(row.CreatedByProviderId),
                UpdatedAtUtc = row.UpdatedAtUtc,
                UpdatedByUserId = row.UpdatedByUserId,
                UpdatedByUserName = UserName(row.UpdatedByUserId),
                UpdatedByProviderId = row.UpdatedByProviderId,
                UpdatedByProviderName = ProviderName(row.UpdatedByProviderId),
                InMyCompany = myId.HasValue,
                MyProviderId = myId,
                AddedByCompanyCount = addedBy
            };
        }

        public async Task<CatalogImportResultDto> ImportAsync(
            CatalogImportRequestDto<CatalogProviderImportRowDto> request,
            CancellationToken cancellationToken)
        {
            var result = new CatalogImportResultDto();

            var categoryExists = await _context.CatalogProviderCategories
                .AnyAsync(c => c.Id == request.CategoryId, cancellationToken);

            if (!categoryExists)
            {
                foreach (var row in request.Rows)
                {
                    result.Rows.Add(Reject(row.RowNumber, CatalogImportErrorCodes.CategoryUnknown, "CategoryId"));
                }

                result.Failed = result.Rows.Count;
                return result;
            }

            var counties = await _context.Counties
                .AsNoTracking()
                .ToDictionaryAsync(c => CatalogText.Normalize(c.Name), c => c.Id, cancellationToken);

            // One entry per distinct entity, holding every row of the file that turned out
            // to be that entity.
            //
            // ⚠️ A repeat is not a rejection. The source files say as much — the hospitals
            // sheet prints "una institución puede aparecer en más de un registro" above its
            // own header, and 381 of its rows are 331 institutions. Rejecting the repeats
            // also made the answer depend on where the batch boundary fell: a twin inside
            // the same batch of 100 was refused, a twin in the next batch was an update.
            // Same file, two different stories.
            var grouped = new Dictionary<string, List<CatalogProviderImportRowDto>>();
            var order = new List<string>();

            foreach (var row in request.Rows)
            {
                var name = row.Name?.Trim();

                if (string.IsNullOrWhiteSpace(name))
                {
                    // Spacing rows, not problems: the assisted living sheet has 2,515 of them,
                    // and reporting each one would bury the real problems.
                    result.Skipped++;
                    continue;
                }

                if (name.Length > 200)
                {
                    result.Rows.Add(Reject(row.RowNumber, CatalogImportErrorCodes.NameTooLong, "Name"));
                    continue;
                }

                var key = CatalogText.BuildMatchKey(request.CategoryId, name, row.Zip);

                if (!grouped.TryGetValue(key, out var bucket))
                {
                    bucket = new List<CatalogProviderImportRowDto>();
                    grouped[key] = bucket;
                    order.Add(key);
                }

                bucket.Add(row);
            }

            var keyList = order.ToList();

            var existing = await _context.CatalogProviders
                .Where(c => keyList.Contains(c.MatchKey))
                .ToDictionaryAsync(c => c.MatchKey, cancellationToken);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;
            var providerId = _currentUser.ProviderId;

            var saved = new List<(CatalogImportRowResultDto Line, CatalogProvider Entity)>();

            foreach (var key in order)
            {
                var rows = grouped[key];
                var isNew = !existing.TryGetValue(key, out var entity);

                if (isNew)
                {
                    entity = new CatalogProvider
                    {
                        CreatedAtUtc = now,
                        CreatedByUserId = userId,
                        CreatedByProviderId = providerId
                    };

                    _context.CatalogProviders.Add(entity);
                }
                else
                {
                    entity!.UpdatedAtUtc = now;
                    entity.UpdatedByUserId = userId;
                    entity.UpdatedByProviderId = providerId;
                }

                // Applied in file order, so the last row wins on anything it states — and
                // FillGaps below keeps what the earlier rows knew and the last one left blank.
                // One record of an institution carries the email, another carries the phone.
                foreach (var row in rows)
                {
                    ApplyImportRow(entity!, row, request, key, counties);
                }

                var line = new CatalogImportRowResultDto
                {
                    RowNumber = rows[0].RowNumber,
                    Outcome = isNew ? CatalogImportOutcome.Created : CatalogImportOutcome.Updated,
                    MergedRowCount = rows.Count,
                    MergedRowNumbers = rows.Count > 1
                        ? rows.Select(r => r.RowNumber).ToList()
                        : null
                };

                result.Rows.Add(line);
                saved.Add((line, entity!));
            }

            result.Merged = result.Rows.Sum(r => Math.Max(0, r.MergedRowCount - 1));

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception)
            {
                // ⚠️ Counts, never values. See the integrator twin.
                _logger.LogError(exception,
                    "Catalog provider import batch {BatchId} failed to save {RowCount} rows.",
                    request.ImportBatchId, order.Count);

                foreach (var line in result.Rows)
                {
                    line.Outcome = CatalogImportOutcome.Rejected;
                    line.ErrorCode = CatalogImportErrorCodes.SaveFailed;
                }

                result.Created = 0;
                result.Updated = 0;
                result.Failed = result.Rows.Count;
                return result;
            }

            foreach (var (line, entity) in saved)
            {
                line.Id = entity.Id;
            }

            result.Created = result.Rows.Count(r => r.Outcome == CatalogImportOutcome.Created);
            result.Updated = result.Rows.Count(r => r.Outcome == CatalogImportOutcome.Updated);
            result.Failed = result.Rows.Count(r => r.Outcome == CatalogImportOutcome.Rejected);

            return result;
        }

        private static void ApplyImportRow(
            CatalogProvider entity,
            CatalogProviderImportRowDto row,
            CatalogImportRequestDto<CatalogProviderImportRowDto> request,
            string key,
            IReadOnlyDictionary<string, int> counties)
        {
            entity.CategoryId = request.CategoryId;
            entity.Name = row.Name!.Trim();
            entity.Address = Keep(entity.Address, Truncate(Clean(row.Address), 300));
            entity.City = Keep(entity.City, Truncate(Clean(row.City), 100));
            entity.CountyRaw = Keep(entity.CountyRaw, Truncate(Clean(row.County), 100));
            entity.State = Keep(entity.State, Truncate(Clean(row.State), 2)) ?? "FL";
            entity.Zip = Keep(entity.Zip, Truncate(Clean(row.Zip), 20));
            entity.Phone = Keep(entity.Phone, Truncate(Clean(row.Phone), 40));
            entity.Email = Keep(entity.Email, Truncate(Clean(row.Email), 200));
            entity.Website = Keep(entity.Website, Truncate(Clean(row.Website), 300));
            entity.ContactName = Keep(entity.ContactName, Truncate(Clean(row.ContactName), 200));
            entity.Npi = Keep(entity.Npi, Truncate(CatalogText.DigitsOnly(row.Npi), 20));
            entity.IsPrimaryNemt = Keep(entity.IsPrimaryNemt, row.IsPrimaryNemt);
            entity.SourceUpdatedOn = Keep(entity.SourceUpdatedOn, row.SourceUpdatedOn);
            entity.EmsLicense = Keep(entity.EmsLicense, Truncate(Clean(row.EmsLicense), 60));
            entity.ServiceLevel = Keep(entity.ServiceLevel, Truncate(Clean(row.ServiceLevel), 60));
            entity.LicenseExpiresOn = Keep(entity.LicenseExpiresOn, row.LicenseExpiresOn);
            entity.PlanSegment = Keep(entity.PlanSegment, Truncate(Clean(row.PlanSegment), 300));
            entity.CoverageArea = Keep(entity.CoverageArea, Truncate(Clean(row.CoverageArea), 300));
            entity.ProviderContact = Keep(entity.ProviderContact, Truncate(Clean(row.ProviderContact), 300));
            entity.EvidenceNote = Keep(entity.EvidenceNote, Truncate(Clean(row.EvidenceNote), 500));

            if (entity.CountyRaw is not null
                && counties.TryGetValue(CatalogText.Normalize(entity.CountyRaw), out var countyId))
            {
                entity.CountyId = countyId;
            }

            if (row.Latitude.HasValue && row.Longitude.HasValue)
            {
                entity.Latitude = row.Latitude;
                entity.Longitude = row.Longitude;
                entity.GeocodeStatus = GeocodeStatus.FromSource;
            }

            entity.SourceFile = Truncate(request.SourceFile, 260);
            entity.SourceRow = row.RowNumber;
            entity.ImportBatchId = request.ImportBatchId;

            entity.MatchKey = key;
            entity.PhoneDigits = CatalogText.DigitsOnly(entity.Phone);
            entity.SearchText = CatalogText.BuildSearchText(
                entity.Name, entity.City, entity.CountyRaw, entity.Zip, entity.Address, entity.ContactName);
        }

        /// <summary>
        /// The incoming value when it says something, otherwise what was already there.
        /// </summary>
        /// <remarks>
        /// ⚠️ An empty cell in these files means "not verified", not "has none" — every one of
        /// the six sheets prints that above its own header. Writing the blank through would
        /// turn one record's silence into the deletion of another record's phone number.
        ///
        /// <para>
        /// The consequence to be aware of: a value really removed at the source is not removed
        /// here by re-importing. Clearing a field is an edit somebody makes on the screen.
        /// </para>
        /// </remarks>
        private static string? Keep(string? current, string? incoming) =>
            string.IsNullOrWhiteSpace(incoming) ? current : incoming;

        private static T? Keep<T>(T? current, T? incoming) where T : struct =>
            incoming ?? current;

        /// <summary>
        /// Cuts a value to what the column holds rather than failing the batch around it.
        /// See the integrator twin.
        /// </summary>
        private static string? Truncate(string? value, int max)
        {
            if (value is null)
            {
                return null;
            }

            return value.Length <= max ? value : value[..max];
        }

        private static CatalogImportRowResultDto Reject(int rowNumber, string code, string? field)
        {
            return new CatalogImportRowResultDto
            {
                RowNumber = rowNumber,
                Outcome = CatalogImportOutcome.Rejected,
                ErrorCode = code,
                Field = field
            };
        }

        public async Task<CatalogGeocodeResultDto> GeocodeAsync(
            IReadOnlyList<int> ids,
            CancellationToken cancellationToken)
        {
            var result = new CatalogGeocodeResultDto();

            if (ids.Count == 0)
            {
                return result;
            }

            var rows = await _context.CatalogProviders
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            var pending = new List<CatalogProvider>();

            foreach (var row in rows)
            {
                if (row.Latitude.HasValue && row.Longitude.HasValue)
                {
                    result.Skipped++;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Address) && string.IsNullOrWhiteSpace(row.Zip))
                {
                    row.GeocodeStatus = GeocodeStatus.NotFound;
                    result.NotFound++;
                    continue;
                }

                pending.Add(row);
            }

            if (pending.Count > 0)
            {
                var addresses = pending
                    .Select(BuildAddressLine)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var response = await _routing.GeocodeBatchAsync(
                    new GeocodeBatchRequestDto { Addresses = addresses },
                    cancellationToken);

                var byAddress = response.Results
                    .GroupBy(r => r.Address, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                var now = DateTime.UtcNow;

                foreach (var row in pending)
                {
                    if (!byAddress.TryGetValue(BuildAddressLine(row), out var answer))
                    {
                        row.GeocodeStatus = GeocodeStatus.Failed;
                        result.Failed++;
                        continue;
                    }

                    if (answer.Status == RoutingContract.Statuses.Ok
                        && answer.Latitude.HasValue
                        && answer.Longitude.HasValue)
                    {
                        row.Latitude = answer.Latitude;
                        row.Longitude = answer.Longitude;
                        row.GeocodeStatus = GeocodeStatus.Resolved;
                        row.GeocodedAtUtc = now;
                        result.Resolved++;
                    }
                    else if (answer.Status == RoutingContract.Statuses.NotFound)
                    {
                        row.GeocodeStatus = GeocodeStatus.NotFound;
                        result.NotFound++;
                    }
                    else
                    {
                        row.GeocodeStatus = GeocodeStatus.Failed;
                        result.Failed++;
                    }
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            return result;
        }

        private static string BuildAddressLine(CatalogProvider row)
        {
            var parts = new[] { row.Address, row.City, row.State, row.Zip }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            return string.Join(", ", parts);
        }

        public async Task<ProviderDto?> BuildCompanyDraftAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var row = await _context.CatalogProviders
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (row is null)
            {
                return null;
            }

            return new ProviderDto
            {
                Name = row.Name,
                Address = row.Address,
                Email = row.Email,
                Phone = row.Phone,
                Website = row.Website,
                ContactName = row.ContactName,
                Comments = row.Comments,
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                CatalogProviderId = row.Id

                // TimeZoneId deliberately left empty. It is the one field nobody can guess from
                // a directory, and a provider without it falls back to the configured default.
                // The form asks for it.
            };
        }
    }
}
