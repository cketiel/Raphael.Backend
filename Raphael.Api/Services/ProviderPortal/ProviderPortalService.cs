using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Catalog;
using Raphael.Api.Services.Routing;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Catalog;
using Raphael.Shared.DTOs.ProviderPortal;
using Raphael.Shared.DTOs.Routing;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Helpers;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.ProviderPortal
{
    /// <summary>Not the caller's, or not an external Provider's user: answered 404.</summary>
    public sealed class ProviderPortalDeniedException : Exception
    {
    }

    /// <summary>What was asked is not possible for this trip or this data: answered 400 with the reason.</summary>
    public sealed class ProviderPortalRuleException : Exception
    {
        public ProviderPortalRuleException(string message) : base(message) { }
    }

    /// <summary>
    /// The Booking portal of a Provider that works outside Raphael (PROVIDER_ACCESS_MODEL.md §3.5):
    /// the clinics that give it trips, the three things it may report about a trip, and the
    /// catalog of Integrators, whose contact details it may correct.
    /// </summary>
    public interface IProviderPortalService
    {
        Task<List<ProviderClinicDto>> GetClinicsAsync(CancellationToken ct);

        Task<int> AcceptAsync(IReadOnlyCollection<int> tripIds, CancellationToken ct);

        Task RejectAsync(int tripId, ProviderTripRejectDto dto, CancellationToken ct);

        Task CompleteAsync(int tripId, ProviderTripCompleteDto dto, CancellationToken ct);

        Task NoShowAsync(int tripId, ProviderTripNoShowDto dto, CancellationToken ct);

        Task<ProviderPortalIntegratorPageDto> SearchIntegratorsAsync(ProviderPortalIntegratorSearchDto request, CancellationToken ct);

        Task<ProviderPortalIntegratorDetailDto?> GetIntegratorAsync(int id, CancellationToken ct);

        Task<ProviderPortalIntegratorDetailDto> EditIntegratorAsync(int id, ProviderPortalIntegratorEditDto request, CancellationToken ct);
    }

    public sealed class ProviderPortalService : IProviderPortalService
    {
        private const int MaxPageSize = 100;
        private const int MaxReasonLength = 500;

        /// <summary>
        /// What a Provider may still turn down: a trip given to it, or one it accepted and cannot do
        /// after all. Saying so is better than not showing up (PROVIDER_ACCESS_MODEL.md §3.7).
        /// </summary>
        private static readonly string[] Rejectable = { TripStatus.Assigned, TripStatus.Accepted };

        /// <summary>
        /// What a Provider may complete or mark as a no-show: a trip it accepted. One it has not
        /// accepted is not yet its own to do (decided 2026-10-11).
        /// </summary>
        private static readonly string[] Reportable = { TripStatus.Accepted, TripStatus.Waiting, TripStatus.Late };

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly ITripService _trips;
        private readonly ICatalogAccountSync _sync;
        private readonly IRoutingService _routing;
        private readonly IOperationClock _clock;
        private readonly Raphael.Api.Services.Notifications.ITripNotificationPublisher _notifications;

        public ProviderPortalService(
            RaphaelContext context,
            ICurrentUserService currentUser,
            ITripService trips,
            ICatalogAccountSync sync,
            IRoutingService routing,
            IOperationClock clock,
            Raphael.Api.Services.Notifications.ITripNotificationPublisher notifications)
        {
            _notifications = notifications;
            _context = context;
            _currentUser = currentUser;
            _trips = trips;
            _sync = sync;
            _routing = routing;
            _clock = clock;
        }

        /// <summary>The caller's Provider. Only an external Provider's user has a portal of its own.</summary>
        private int ProviderId =>
            _currentUser.IsExternalProvider && _currentUser.ProviderId is int id ? id : throw new ProviderPortalDeniedException();

        /// <summary>The clinics (Integrator accounts) that contracted the caller's Provider.</summary>
        private IQueryable<Integrator> MyClinics(int providerId)
        {
            var catalogId = _context.Providers.Where(p => p.Id == providerId).Select(p => p.CatalogProviderId);
            var integratorIds = _context.IntegratorProviders
                .Where(r => catalogId.Contains(r.CatalogProviderId))
                .Select(r => r.IntegratorId);
            return _context.Integrators.AsNoTracking().Where(i => integratorIds.Contains(i.Id));
        }

        public async Task<List<ProviderClinicDto>> GetClinicsAsync(CancellationToken ct)
        {
            var providerId = ProviderId;
            return await MyClinics(providerId)
                .OrderBy(i => i.Name)
                .Select(i => new ProviderClinicDto { IntegratorId = i.Id, Name = i.Name })
                .ToListAsync(ct);
        }

        // ------------------------------------------------------------------ the three reports

        /// <summary>One of the caller's trips. The Trip filter already keeps it to the Provider; asked again here.</summary>
        private async Task<Trip> MyTripAsync(int tripId, CancellationToken ct)
        {
            var providerId = ProviderId;
            return await _context.Trips.FirstOrDefaultAsync(t => t.Id == tripId && t.ProviderId == providerId, ct)
                ?? throw new ProviderPortalDeniedException();
        }

        private string Who => $"Provider - {_currentUser.UserName ?? "user"}";

        private static string Reason(string? reason)
        {
            var text = reason?.Trim() ?? "";
            if (text.Length == 0) throw new ProviderPortalRuleException("A reason is required.");
            if (text.Length > MaxReasonLength) throw new ProviderPortalRuleException($"The reason can have at most {MaxReasonLength} characters.");
            return text;
        }

        private void Stamp(Trip trip, ProviderOutcome outcome, string? reason)
        {
            trip.ProviderOutcome = outcome;
            trip.ProviderOutcomeReason = reason;
            trip.ProviderOutcomeAtUtc = DateTime.UtcNow;
            trip.ProviderOutcomeByUserId = _currentUser.UserId;
        }

        private void History(Trip trip, string field, string? prior, string? next) =>
            _context.TripHistories.Add(new TripHistory
            {
                TripId = trip.Id,
                User = Who,
                Field = field,
                PriorValue = prior,
                NewValue = next,
                ChangeDate = DateTime.UtcNow
            });

        /// <summary>
        /// The Provider commits to these trips of its own: from Assigned to Accepted. From then on the
        /// clinic can no longer give them to another Provider, and the Provider may export them.
        /// Trips that are not Assigned are skipped; the count says how many were accepted.
        /// </summary>
        public async Task<int> AcceptAsync(IReadOnlyCollection<int> tripIds, CancellationToken ct)
        {
            var providerId = ProviderId;
            if (tripIds.Count == 0) return 0;
            if (tripIds.Count > 500) throw new ProviderPortalRuleException("Accept at most 500 trips at a time.");

            var trips = await _context.Trips
                .Where(t => tripIds.Contains(t.Id) && t.ProviderId == providerId && !t.IsCancelled && t.Status == TripStatus.Assigned)
                .ToListAsync(ct);
            foreach (var trip in trips)
            {
                trip.Status = TripStatus.Accepted;
                History(trip, "Status", TripStatus.Assigned, TripStatus.Accepted);
            }
            await _context.SaveChangesAsync(ct);
            return trips.Count;
        }

        /// <remarks>
        /// The trip keeps the Provider and becomes Rejected: the clinic must give it to another
        /// Provider or to Raphael, which puts it back to Assigned. The clinic and the office are told
        /// (TRIP_REJECTED).
        /// </remarks>
        public async Task RejectAsync(int tripId, ProviderTripRejectDto dto, CancellationToken ct)
        {
            var reason = Reason(dto.Reason);
            var trip = await MyTripAsync(tripId, ct);

            if (trip.IsCancelled || !Rejectable.Contains(trip.Status))
                throw new ProviderPortalRuleException("This trip has already been routed, started or closed: it can no longer be rejected.");

            var prior = trip.Status;
            trip.Status = TripStatus.Rejected;
            Stamp(trip, ProviderOutcome.Rejected, reason);
            History(trip, "Status", prior, $"{TripStatus.Rejected}: {reason}");
            await _context.SaveChangesAsync(ct);

            var providerName = await _context.Providers.Where(p => p.Id == trip.ProviderId).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "Its provider";
            await _notifications.TripRejectedAsync(trip, providerName, reason, ct);
        }

        /// <remarks>
        /// The real times go on the trip: a trip done outside Raphael has no route, so no Schedules,
        /// and the production report reads them from here to bill it.
        /// </remarks>
        public async Task CompleteAsync(int tripId, ProviderTripCompleteDto dto, CancellationToken ct)
        {
            var trip = await ReportableTripAsync(tripId, ct);

            var day = TimeSpan.FromDays(1);
            bool InDay(TimeSpan? t) => t is null || (t.Value >= TimeSpan.Zero && t.Value < day);
            if (!InDay(dto.PickedUpAt) || !InDay(dto.DroppedOffAt) || !InDay(dto.PickupArrivedAt) || !InDay(dto.DropoffArrivedAt))
                throw new ProviderPortalRuleException("The times must be within the trip's day.");
            if (dto.DroppedOffAt < dto.PickedUpAt)
                throw new ProviderPortalRuleException("The drop-off cannot be earlier than the pickup.");
            if (dto.PickupArrivedAt > dto.PickedUpAt)
                throw new ProviderPortalRuleException("The arrival at the pickup cannot be later than the pickup.");
            if (dto.DropoffArrivedAt is TimeSpan arrived && (arrived > dto.DroppedOffAt || arrived < dto.PickedUpAt))
                throw new ProviderPortalRuleException("The arrival at the drop-off must be between the pickup and the drop-off.");

            var prior = trip.Status;
            trip.ProviderPickupArrivedAt = dto.PickupArrivedAt;
            trip.ProviderPickedUpAt = dto.PickedUpAt;
            trip.ProviderDropoffArrivedAt = dto.DropoffArrivedAt;
            trip.ProviderDroppedOffAt = dto.DroppedOffAt;
            trip.Status = TripStatus.Finished;
            Stamp(trip, ProviderOutcome.Completed, null);
            History(trip, "Status", prior, $"{TripStatus.Finished} (picked up {dto.PickedUpAt:hh\\:mm}, dropped off {dto.DroppedOffAt:hh\\:mm})");
            await _context.SaveChangesAsync(ct);
        }

        /// <remarks>
        /// The same as a driver's no-show (TripService.CancelByDriverAsync): cancelled with the reason,
        /// the patient's other pending trips of that day (the return) with it, and the clinic and the
        /// patient told as for a driver's (decided 2026-10-11).
        /// </remarks>
        public async Task NoShowAsync(int tripId, ProviderTripNoShowDto dto, CancellationToken ct)
        {
            var reason = Reason(dto.Reason);
            var trip = await ReportableTripAsync(tripId, ct);

            if (!await _trips.CancelByDriverAsync(trip.Id, reason, Who))
                throw new ProviderPortalRuleException("This trip can no longer be marked as a no-show.");

            // CancelByDriverAsync saved on this same context: the tracked trip is already cancelled.
            Stamp(trip, ProviderOutcome.NoShow, reason);
            await _context.SaveChangesAsync(ct);
        }

        /// <summary>A trip of the caller's that can be completed or marked no-show: accepted, still open, not in the future.</summary>
        private async Task<Trip> ReportableTripAsync(int tripId, CancellationToken ct)
        {
            var trip = await MyTripAsync(tripId, ct);

            if (trip.IsCancelled || !Reportable.Contains(trip.Status))
                throw new ProviderPortalRuleException(trip.Status == TripStatus.Assigned
                    ? "Accept this trip before reporting its result."
                    : "This trip is already finished, cancelled or rejected.");
            // The business's own day (TIME_POLICY): a trip of tomorrow has not happened yet.
            if (trip.Date.Date > _clock.NowFor(trip.ProviderId).Date)
                throw new ProviderPortalRuleException("A trip of a day still to come cannot be reported yet.");
            return trip;
        }

        // ------------------------------------------------------------------ the catalog of Integrators

        private IQueryable<int> MyClinicCatalogIds(int providerId) =>
            MyClinics(providerId).Where(i => i.CatalogIntegratorId != null).Select(i => i.CatalogIntegratorId!.Value);

        public async Task<ProviderPortalIntegratorPageDto> SearchIntegratorsAsync(ProviderPortalIntegratorSearchDto request, CancellationToken ct)
        {
            var providerId = ProviderId;
            var mine = MyClinicCatalogIds(providerId);

            var query = _context.CatalogIntegrators.AsNoTracking().Where(c => c.IsActive || mine.Contains(c.Id));

            foreach (var term in CatalogText.SplitTerms(request.Term))
            {
                var needle = term;
                query = CatalogText.LooksLikePhone(needle)
                    ? query.Where(c => c.SearchText.Contains(needle) || (c.PhoneDigits != null && c.PhoneDigits.Contains(needle)))
                    : query.Where(c => c.SearchText.Contains(needle));
            }

            if (request.Mine == true) query = query.Where(c => mine.Contains(c.Id));

            // Counted before its own filter, so choosing a group still shows the others.
            var groups = await query
                .GroupBy(c => new { c.CategoryId, c.Category.NameEn })
                .Select(g => new CatalogFacetValueDto { Id = g.Key.CategoryId, Label = g.Key.NameEn, Count = g.Count() })
                .ToListAsync(ct);

            if (request.GroupId is int groupId) query = query.Where(c => c.CategoryId == groupId);

            var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
            var pageNumber = Math.Max(1, request.PageNumber);
            var total = await query.CountAsync(ct);
            var mineCount = await query.CountAsync(c => mine.Contains(c.Id), ct);

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new ProviderPortalIntegratorRowDto
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
                    IsMyClinic = mine.Contains(c.Id)
                })
                .ToListAsync(ct);

            return new ProviderPortalIntegratorPageDto
            {
                Items = items,
                TotalCount = total,
                PageNumber = pageNumber,
                PageSize = pageSize,
                MineCount = mineCount,
                Groups = groups.OrderBy(g => g.Label).ToList()
            };
        }

        public async Task<ProviderPortalIntegratorDetailDto?> GetIntegratorAsync(int id, CancellationToken ct)
        {
            var providerId = ProviderId;
            var mine = MyClinicCatalogIds(providerId);

            return await _context.CatalogIntegrators.AsNoTracking()
                .Where(c => c.Id == id && (c.IsActive || mine.Contains(c.Id)))
                .Select(c => new ProviderPortalIntegratorDetailDto
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
                    FacilityType = c.FacilityType,
                    Beds = c.Beds,
                    ChainName = c.ChainName,
                    Latitude = c.Latitude,
                    Longitude = c.Longitude,
                    IsActive = c.IsActive,
                    IsMyClinic = mine.Contains(c.Id)
                })
                .FirstOrDefaultAsync(ct);
        }

        /// <remarks>
        /// Any Integrator of the catalog, its contact details only (decided 2026-10-11, the same rule a
        /// clinic has over Providers). The catalog is shared: the edit is authored, and every account
        /// made out of the entry takes it (ICatalogAccountSync).
        /// </remarks>
        public async Task<ProviderPortalIntegratorDetailDto> EditIntegratorAsync(int id, ProviderPortalIntegratorEditDto request, CancellationToken ct)
        {
            var providerId = ProviderId;
            var mine = MyClinicCatalogIds(providerId);
            if (!await _context.CatalogIntegrators.AnyAsync(c => c.Id == id && (c.IsActive || mine.Contains(c.Id)), ct))
                throw new ProviderPortalDeniedException();
            if (string.IsNullOrWhiteSpace(request.Name)) throw new ProviderPortalRuleException("The name is required.");

            var row = await _context.CatalogIntegrators.FirstAsync(c => c.Id == id, ct);

            // ⚠️ Everything is worked out before the row is touched: geocoding saves its cache on this
            // same context, and that save would carry a half-applied row with it (PortalCatalogService).
            var name = request.Name.Trim();
            var address = Clean(request.Address);
            var city = Clean(request.City);
            var state = Clean(request.State);
            var zip = Clean(request.Zip);

            var matchKey = CatalogText.BuildMatchKey(row.CategoryId, name, zip);
            if (await _context.CatalogIntegrators.AnyAsync(c => c.Id != row.Id && c.MatchKey == matchKey, ct))
                throw new CatalogConflictException();

            var placeChanged = !Same(row.Address, address) || !Same(row.City, city) || !Same(row.State, state) || !Same(row.Zip, zip);
            GeocodeResultDto? found = null;
            if (placeChanged)
            {
                var line = RouteCacheKey.ComposeAddress(address, city, state, zip);
                found = string.IsNullOrWhiteSpace(line) ? null : await _routing.GeocodeAsync(new GeocodeRequestDto { Address = line }, ct);
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

            await _sync.SpreadAsync(row, ct);
            await _context.SaveChangesAsync(ct);

            return (await GetIntegratorAsync(id, ct))!;
        }

        private static bool Same(string? a, string? b) =>
            string.Equals(Clean(a), Clean(b), StringComparison.OrdinalIgnoreCase);

        private static string? Clean(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
