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
    /// <inheritdoc cref="ICatalogIntegratorService"/>
    public sealed class CatalogIntegratorService : ICatalogIntegratorService
    {
        /// <summary>
        /// Never serve more than this in one page, whatever the client asks for. A catalog of
        /// thousands of rows and a client that asks for all of them is how a screen freezes.
        /// </summary>
        private const int MaxPageSize = 200;

        /// <summary>How many near-misses to offer when a search finds nothing.</summary>
        private const int MaxSuggestions = 5;

        /// <summary>Longest list of facet values worth sending. Beyond this nobody reads it.</summary>
        private const int MaxFacetValues = 60;

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IRoutingService _routing;
        private readonly ILogger<CatalogIntegratorService> _logger;

        public CatalogIntegratorService(
            RaphaelContext context,
            ICurrentUserService currentUser,
            IRoutingService routing,
            ILogger<CatalogIntegratorService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _routing = routing;
            _logger = logger;
        }

        /// <summary>
        /// The rows of <c>Integrators</c> that belong to whoever is asking.
        /// </summary>
        /// <remarks>
        /// ⚠️ The branch is not decoration. <c>OwnerProviderId == providerId</c> with a null
        /// parameter becomes <c>OwnerProviderId = NULL</c> in SQL, which is never true, and the
        /// general broker — the one whose provider is null — would silently see none of its own
        /// rows as its own. The comparison against null has to be written as <c>IS NULL</c>.
        /// </remarks>
        private IQueryable<Integrator> MyIntegrators()
        {
            var providerId = _currentUser.ProviderId;

            return providerId.HasValue
                ? _context.Integrators.Where(i => i.OwnerProviderId == providerId.Value)
                : _context.Integrators.Where(i => i.OwnerProviderId == null);
        }

        /// <summary>The general broker, and only it, is told who else added an entity.</summary>
        private bool CallerIsBroker => _currentUser.ProviderId is null;

        public async Task<CatalogPageDto<CatalogIntegratorListItemDto>> SearchAsync(
            CatalogSearchRequestDto request,
            CancellationToken cancellationToken)
        {
            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
            var pageNumber = Math.Max(request.PageNumber, 1);

            var mine = MyIntegrators();
            var query = _context.CatalogIntegrators.AsNoTracking().AsQueryable();

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
                .Select(c => new CatalogIntegratorListItemDto
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
                    FacilityType = c.FacilityType,
                    Beds = c.Beds,
                    IsActive = c.IsActive,
                    HasCoordinates = c.Latitude != null && c.Longitude != null,
                    MyIntegratorId = mine
                        .Where(i => i.CatalogIntegratorId == c.Id)
                        .Select(i => (int?)i.Id)
                        .FirstOrDefault(),
                    AddedByCompanyCount = _context.Integrators
                        .Count(i => i.CatalogIntegratorId == c.Id)
                })
                .ToListAsync(cancellationToken);

            foreach (var item in items)
            {
                item.InMyCompany = item.MyIntegratorId.HasValue;

                // Computed for everyone because branching the projection would cost a second
                // query; dropped here because a provider has no business knowing who else is
                // talking to whom.
                if (!CallerIsBroker)
                {
                    item.AddedByCompanyCount = null;
                }
            }

            var page = new CatalogPageDto<CatalogIntegratorListItemDto>
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

        private IQueryable<CatalogIntegrator> ApplyFilters(
            IQueryable<CatalogIntegrator> query,
            CatalogSearchRequestDto request,
            IQueryable<Integrator> mine)
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
                query = query.Where(c => mine.Any(i => i.CatalogIntegratorId == c.Id));
            }
            else if (request.InMyCompany == false)
            {
                query = query.Where(c => !mine.Any(i => i.CatalogIntegratorId == c.Id));
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

        /// <summary>
        /// Every term has to match something. That is what lets "harborview altamonte" find the
        /// row whose name holds one word and whose city holds the other.
        /// </summary>
        /// <remarks>
        /// A term made only of digits is also tried against the phone, so the same row is found
        /// by "(407) 786-5637", "407-786-5637" and "4077865637" alike.
        /// </remarks>
        private static IQueryable<CatalogIntegrator> ApplyTerms(
            IQueryable<CatalogIntegrator> query,
            IReadOnlyList<string> terms)
        {
            foreach (var term in terms)
            {
                var needle = term;

                query = CatalogText.LooksLikePhone(needle)
                    ? query.Where(c => c.SearchText.Contains(needle)
                        || (c.PhoneDigits != null && c.PhoneDigits.Contains(needle)))
                    : query.Where(c => c.SearchText.Contains(needle));
            }

            return query;
        }

        /// <summary>
        /// Relevance, not the alphabet. <c>SearchText</c> begins with the name, so a row whose
        /// search text starts with what was typed is a row whose name starts with it.
        /// </summary>
        private static IQueryable<CatalogIntegrator> ApplySort(
            IQueryable<CatalogIntegrator> query,
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

        /// <summary>
        /// What the search could still be narrowed by, each with its count, computed over the
        /// same rows the search matched but ignoring the facet's own filter.
        /// </summary>
        private async Task<CatalogFacetsDto> BuildFacetsAsync(
            CatalogSearchRequestDto request,
            IQueryable<Integrator> mine,
            IReadOnlyList<string> terms,
            CancellationToken cancellationToken)
        {
            // A facet that counted itself would always show its own value and nothing else, so
            // each one is counted over the search with that filter lifted.
            var baseQuery = _context.CatalogIntegrators.AsNoTracking().AsQueryable();

            if (!request.IncludeInactive)
            {
                baseQuery = baseQuery.Where(c => c.IsActive);
            }

            baseQuery = ApplyTerms(baseQuery, terms);

            var withoutCategory = ApplyFilters(
                baseQuery, CloneWithout(request, clearCategory: true), mine);

            var withoutCounty = ApplyFilters(
                baseQuery, CloneWithout(request, clearCounty: true), mine);

            var withoutCity = ApplyFilters(
                baseQuery, CloneWithout(request, clearCity: true), mine);

            var withoutOwnership = ApplyFilters(
                baseQuery, CloneWithout(request, clearOwnership: true), mine);

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
                .CountAsync(c => mine.Any(i => i.CatalogIntegratorId == c.Id), cancellationToken);

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
        /// </summary>
        /// <remarks>
        /// A prefix rather than SQL Server's <c>DIFFERENCE</c>, which EF Core does not map and
        /// which would mean raw SQL for a convenience. It catches the common typo — the one in
        /// the middle or at the end of a word, "harborviu" for "harborview" — and misses the one
        /// in the first letters. That is an honest trade for a fallback that only ever runs when
        /// the screen would otherwise say nothing at all.
        /// </remarks>
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

            var query = _context.CatalogIntegrators.AsNoTracking().AsQueryable();

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

        public async Task<CatalogIntegratorDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var row = await _context.CatalogIntegrators
                .AsNoTracking()
                .Include(c => c.Category)
                .Include(c => c.County)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            return row is null ? null : await ToDetailAsync(row, cancellationToken);
        }

        public async Task<CatalogIntegratorDto> CreateAsync(
            CatalogIntegratorSaveDto request,
            CancellationToken cancellationToken)
        {
            var row = new CatalogIntegrator
            {
                CreatedAtUtc = DateTime.UtcNow,
                CreatedByUserId = _currentUser.UserId,
                CreatedByProviderId = _currentUser.ProviderId
            };

            Apply(request, row);

            _context.CatalogIntegrators.Add(row);
            await _context.SaveChangesAsync(cancellationToken);

            return (await GetByIdAsync(row.Id, cancellationToken))!;
        }

        public async Task<CatalogIntegratorDto?> UpdateAsync(
            int id,
            CatalogIntegratorSaveDto request,
            CancellationToken cancellationToken)
        {
            var row = await _context.CatalogIntegrators
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
        /// Copies the editable fields and rebuilds everything derived from them.
        /// </summary>
        /// <remarks>
        /// The derived columns are rebuilt here and nowhere else. A row saved with a new phone
        /// and a stale <c>PhoneDigits</c> would stop being findable by its own number, and
        /// nothing would report an error.
        /// </remarks>
        private static void Apply(CatalogIntegratorSaveDto request, CatalogIntegrator row)
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
            row.FacilityType = Clean(request.FacilityType);
            row.Beds = request.Beds;
            row.ChainName = Clean(request.ChainName);
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

        private async Task<CatalogIntegratorDto> ToDetailAsync(
            CatalogIntegrator row,
            CancellationToken cancellationToken)
        {
            var myId = await MyIntegrators()
                .Where(i => i.CatalogIntegratorId == row.Id)
                .Select(i => (int?)i.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var addedBy = CallerIsBroker
                ? await _context.Integrators
                    .CountAsync(i => i.CatalogIntegratorId == row.Id, cancellationToken)
                : (int?)null;

            var people = await ResolveNamesAsync(
                new[] { row.CreatedByUserId, row.UpdatedByUserId },
                new[] { row.CreatedByProviderId, row.UpdatedByProviderId },
                cancellationToken);

            return new CatalogIntegratorDto
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
                FacilityType = row.FacilityType,
                Beds = row.Beds,
                ChainName = row.ChainName,
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
                CreatedByUserName = people.UserName(row.CreatedByUserId),
                CreatedByProviderId = row.CreatedByProviderId,
                CreatedByProviderName = people.ProviderName(row.CreatedByProviderId),
                UpdatedAtUtc = row.UpdatedAtUtc,
                UpdatedByUserId = row.UpdatedByUserId,
                UpdatedByUserName = people.UserName(row.UpdatedByUserId),
                UpdatedByProviderId = row.UpdatedByProviderId,
                UpdatedByProviderName = people.ProviderName(row.UpdatedByProviderId),
                InMyCompany = myId.HasValue,
                MyIntegratorId = myId,
                AddedByCompanyCount = addedBy
            };
        }

        /// <summary>Who the audit ids belong to, in two queries rather than four.</summary>
        private async Task<AuditNames> ResolveNamesAsync(
            IEnumerable<int?> userIds,
            IEnumerable<int?> providerIds,
            CancellationToken cancellationToken)
        {
            var users = userIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var providers = providerIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

            var userNames = users.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Users
                    .AsNoTracking()
                    .Where(u => users.Contains(u.Id))
                    .ToDictionaryAsync(u => u.Id, u => u.Username, cancellationToken);

            var providerNames = providers.Count == 0
                ? new Dictionary<int, string>()
                : await _context.Providers
                    .AsNoTracking()
                    .Where(p => providers.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            return new AuditNames(userNames, providerNames);
        }

        private sealed record AuditNames(
            Dictionary<int, string> Users,
            Dictionary<int, string> Providers)
        {
            public string? UserName(int? id) =>
                id.HasValue && Users.TryGetValue(id.Value, out var name) ? name : null;

            /// <summary>Null provider means the general broker, and that is worth saying so.</summary>
            public string? ProviderName(int? id) =>
                id.HasValue && Providers.TryGetValue(id.Value, out var name) ? name : null;
        }

        public async Task<CatalogImportResultDto> ImportAsync(
            CatalogImportRequestDto<CatalogIntegratorImportRowDto> request,
            CancellationToken cancellationToken)
        {
            var result = new CatalogImportResultDto();

            var categoryExists = await _context.CatalogIntegratorCategories
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

            // Everything the batch is about to touch, fetched once. Row by row this would be one
            // round trip per row, which over a 2,000-row file is the difference between seconds
            // and minutes.
            // One entry per distinct entity, holding every row of the file that turned out
            // to be that entity.
            //
            // ⚠️ A repeat is not a rejection. The source files say as much — the hospitals
            // sheet prints "una institución puede aparecer en más de un registro" above its
            // own header, and 381 of its rows are 331 institutions. Rejecting the repeats
            // also made the answer depend on where the batch boundary fell: a twin inside
            // the same batch of 100 was refused, a twin in the next batch was an update.
            // Same file, two different stories.
            var grouped = new Dictionary<string, List<CatalogIntegratorImportRowDto>>();
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
                    bucket = new List<CatalogIntegratorImportRowDto>();
                    grouped[key] = bucket;
                    order.Add(key);
                }

                bucket.Add(row);
            }

            var keyList = order.ToList();

            var existing = await _context.CatalogIntegrators
                .Where(c => keyList.Contains(c.MatchKey))
                .ToDictionaryAsync(c => c.MatchKey, cancellationToken);

            var now = DateTime.UtcNow;
            var userId = _currentUser.UserId;
            var providerId = _currentUser.ProviderId;

            // Kept so the response can carry the id of every row, including the ones that did
            // not exist a moment ago and only get an id once SaveChanges returns.
            var saved = new List<(CatalogImportRowResultDto Line, CatalogIntegrator Entity)>();

            foreach (var key in order)
            {
                var rows = grouped[key];
                var isNew = !existing.TryGetValue(key, out var entity);

                if (isNew)
                {
                    entity = new CatalogIntegrator
                    {
                        CreatedAtUtc = now,
                        CreatedByUserId = userId,
                        CreatedByProviderId = providerId
                    };

                    _context.CatalogIntegrators.Add(entity);
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
                // ⚠️ No row values in the log. A catalog row is a company, not a patient, but the
                // habit of logging what failed is how patient data ends up in a log file.
                _logger.LogError(exception,
                    "Catalog integrator import batch {BatchId} failed to save {RowCount} rows.",
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
            CatalogIntegrator entity,
            CatalogIntegratorImportRowDto row,
            CatalogImportRequestDto<CatalogIntegratorImportRowDto> request,
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
            entity.FacilityType = Keep(entity.FacilityType, Truncate(Clean(row.FacilityType), 150));
            entity.Beds = Keep(entity.Beds, row.Beds);
            entity.ChainName = Keep(entity.ChainName, Truncate(Clean(row.ChainName), 200));

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
        /// Cuts a value to what the column holds instead of letting the whole batch fail.
        /// </summary>
        /// <remarks>
        /// A source file is not ours to correct. One over-long address must not reject the
        /// thousand rows travelling with it, and a truncated address is still a usable address.
        /// </remarks>
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

            var rows = await _context.CatalogIntegrators
                .Where(c => ids.Contains(c.Id))
                .ToListAsync(cancellationToken);

            var pending = new List<CatalogIntegrator>();

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
                // One call for the lot, deduplicated inside the proxy. A loop calling once per
                // row is the shape RE-006 removed from the codebase.
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

        private static string BuildAddressLine(CatalogIntegrator row)
        {
            var parts = new[] { row.Address, row.City, row.State, row.Zip }
                .Where(p => !string.IsNullOrWhiteSpace(p));

            return string.Join(", ", parts);
        }

        public async Task<IntegratorDto?> BuildCompanyDraftAsync(
            int id,
            CancellationToken cancellationToken)
        {
            var row = await _context.CatalogIntegrators
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (row is null)
            {
                return null;
            }

            return new IntegratorDto
            {
                Name = row.Name,
                IsActive = true,
                Phone = row.Phone,
                Website = row.Website,
                Email = row.Email,
                Address = row.Address,
                ContactName = row.ContactName,
                Comments = row.Comments,
                Latitude = row.Latitude,
                Longitude = row.Longitude,
                CatalogIntegratorId = row.Id
            };
        }
    }
}
