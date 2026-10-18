using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Trips;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Places;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Entities.Places;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Places
{
    public interface ITripNotesService
    {
        /// <summary>
        /// Works a batch of trips (RE-054): finds or creates the places of both ends, learns what each
        /// file said as notes to review, and, for <see cref="NotesWork.Compose"/>, writes the comment
        /// the driver reads. One load of places and notes per batch; never calls Google. Called by the
        /// notes worker and by the notes window, never by an import, a save, routing or finishing a trip.
        /// </summary>
        /// <returns>How many trips could not be worked.</returns>
        Task<int> ProcessTripsAsync(IReadOnlyCollection<int> tripIds, NotesWork work, CancellationToken cancellationToken = default);

        /// <summary>Today's and tomorrow's open trips, for the nightly pass.</summary>
        Task<List<int>> TripsForNightlyPassAsync(CancellationToken cancellationToken = default);

        Task<TripNotesDto?> GetAsync(int tripId);
        Task<TripNotesDto?> SetTripNoteAsync(int tripId, TripSide side, string? text);
        Task<TripNotesDto?> AddPlaceNoteAsync(int tripId, TripSide side, NoteUpsertDto dto);
        Task<TripNotesDto?> AddCustomerNoteAsync(int tripId, NoteUpsertDto dto);
        Task<TripNotesDto?> UpdatePlaceNoteAsync(int tripId, int noteId, NoteUpsertDto dto);
        Task<TripNotesDto?> UpdateCustomerNoteAsync(int tripId, int noteId, NoteUpsertDto dto);
        Task<TripNotesDto?> RemovePlaceNoteAsync(int tripId, int noteId);
        Task<TripNotesDto?> RemoveCustomerNoteAsync(int tripId, int noteId);
        Task<TripNotesDto?> PromoteTripNoteAsync(int tripId, TripSide side, PromoteTripNoteDto dto);
        Task<TripNotesDto?> LinkPlaceAsync(int tripId, TripSide side, int placeId);
        Task<TripNotesDto?> UpdatePlaceAsync(int tripId, TripSide side, PlaceUpdateDto dto);
        Task<List<PlaceSummaryDto>> SearchPlacesAsync(string? search, int take);
        Task<NotesPreviewDto> PreviewAsync(NotesPreviewRequestDto request);

        /// <summary>How many notes still to review each trip has, for the grids. Trips with none are absent.</summary>
        Task<Dictionary<int, int>> CountNotesToReviewAsync(IReadOnlyCollection<int> tripIds);

        /// <summary>
        /// The comments a party outside the office may see: what the file, clinic or integrator sent,
        /// never the office's notes (PHI). A trip from before RE-054 keeps the comment it had.
        /// </summary>
        Task<Dictionary<int, (string? Pickup, string? Dropoff)>> GetExternalCommentsAsync(IEnumerable<Trip> trips);

        /// <summary>Same rule, written over trips already read for a clinic or an external Provider.</summary>
        Task ApplyExternalCommentsAsync(IEnumerable<Raphael.Shared.DTOs.TripReadDto> trips);
    }

    /// <summary>
    /// What an import, a clinic booking, the integration API or the trip form store for the notes
    /// (RE-054): the source's own text, apart from the comment. A couple of plain writes in the same
    /// save; no lookup of places, no reading of notes. The notes worker does the rest later.
    /// </summary>
    public static class TripFileComments
    {
        /// <summary>
        /// A file, a clinic or an integrator sent this trip with these comments. A new trip carries
        /// them as its comment, as always. A trip already worked keeps its composed comment: only the
        /// source's text changes, and the worker composes again. That is what stops a re-import from
        /// erasing what the office added.
        /// </summary>
        public static async Task StashAsync(RaphaelContext context, Trip trip, bool isNew, string? pickup, string? dropoff)
        {
            pickup = Clean(pickup);
            dropoff = Clean(dropoff);

            if (isNew)
            {
                trip.PickupComment = pickup;
                trip.DropoffComment = dropoff;
                context.TripStopNotes.Add(new TripStopNote { Trip = trip, Side = TripSide.Pickup, FileComment = pickup });
                context.TripStopNotes.Add(new TripStopNote { Trip = trip, Side = TripSide.Dropoff, FileComment = dropoff });
                return;
            }

            var rows = await context.TripStopNotes.Where(s => s.TripId == trip.Id).ToListAsync();
            foreach (var (side, text) in new[] { (TripSide.Pickup, pickup), (TripSide.Dropoff, dropoff) })
            {
                var row = rows.FirstOrDefault(r => r.Side == side);
                if (row == null)
                {
                    // A trip from before RE-054: its comment may hold what the office typed over the
                    // years. It stays; the worker keeps those lines as the trip's own note.
                    context.TripStopNotes.Add(new TripStopNote { Trip = trip, Side = side, FileComment = text });
                    continue;
                }

                var neverWorked = row.ProcessedAtUtc == null && row.ComposedHash == null;
                row.FileComment = text;
                row.ProcessedAtUtc = null;
                // Nothing composed yet, so nothing of the office's to protect: the comment is the file's.
                if (neverWorked)
                {
                    if (side == TripSide.Pickup) trip.PickupComment = text;
                    else trip.DropoffComment = text;
                }
            }
        }

        /// <summary>A trip typed in the office: no source text, the comment is the office's.</summary>
        public static void StashOffice(RaphaelContext context, Trip trip)
        {
            context.TripStopNotes.Add(new TripStopNote { Trip = trip, Side = TripSide.Pickup });
            context.TripStopNotes.Add(new TripStopNote { Trip = trip, Side = TripSide.Dropoff });
        }

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    }

    /// <summary>Notes of places and patients, and the comment each trip end carries for the driver (RE-054).</summary>
    /// <remarks>
    /// <para>
    /// The company a place, a patient note or a learnt line belongs to is the trip's: the tenant
    /// that carries it, or Raphael (null). It is set explicitly, because the worker runs with no user
    /// and the context's filters would otherwise see only Raphael's rows.
    /// </para>
    /// <para>PHI: note texts and home addresses never reach a log line, an exception message or a URL.</para>
    /// </remarks>
    public sealed class TripNotesService : ITripNotesService
    {
        /// <summary>A stop this close to an entrance of a place is at that entrance.</summary>
        public const double SameEntranceMeters = 40;

        /// <summary>A stop this close to a patient's home, or their record's position, is that home.</summary>
        public const double HomeMeters = 60;

        /// <summary>A new facility this close to a catalog entry takes its name.</summary>
        public const double CatalogMeters = 60;

        private const int DriverRoleId = 2;

        private static readonly string[] ClosedStatuses =
        {
            TripStatus.Finished, TripStatus.Canceled, TripStatus.Billed, TripStatus.Payed,
        };

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IOperationClock _clock;
        private readonly ITripChangeGuard _guard;
        private readonly ITripNotesQueue _queue;
        private readonly ILogger<TripNotesService> _logger;

        public TripNotesService(RaphaelContext context, ICurrentUserService currentUser, IOperationClock clock,
            ITripChangeGuard guard, ITripNotesQueue queue, ILogger<TripNotesService> logger)
        {
            _context = context;
            _currentUser = currentUser;
            _clock = clock;
            _guard = guard;
            _queue = queue;
            _logger = logger;
        }

        // =====================================================================================
        // The worker's job
        // =====================================================================================

        public async Task<int> ProcessTripsAsync(IReadOnlyCollection<int> tripIds, NotesWork work, CancellationToken cancellationToken = default)
        {
            if (tripIds.Count == 0) return 0;
            try
            {
                await ProcessBatchAsync(tripIds, work, cancellationToken);
                return 0;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One by one, so one bad trip (or a place another process created at the same moment)
                // does not hold back the rest. The retry reads what the other process saved.
                _logger.LogWarning(ex, "Notes: batch of {Count} trip(s) failed, retrying one by one", tripIds.Count);
                _context.ChangeTracker.Clear();
                var failed = 0;
                foreach (var id in tripIds)
                {
                    try
                    {
                        await ProcessBatchAsync(new[] { id }, work, cancellationToken);
                    }
                    catch (Exception one) when (one is not OperationCanceledException)
                    {
                        failed++;
                        _logger.LogWarning(one, "Notes: trip {TripId} could not be worked", id);
                        _context.ChangeTracker.Clear();
                    }
                }
                return failed;
            }
        }

        public async Task<List<int>> TripsForNightlyPassAsync(CancellationToken cancellationToken = default)
        {
            var today = _clock.TodayFor(null);
            var tomorrow = today.AddDays(1);
            return await _context.Trips.IgnoreQueryFilters().AsNoTracking()
                .Where(t => t.Date >= today && t.Date <= tomorrow && !t.IsCancelled && !ClosedStatuses.Contains(t.Status))
                .Select(t => t.Id).ToListAsync(cancellationToken);
        }

        private async Task ProcessBatchAsync(IReadOnlyCollection<int> tripIds, NotesWork work, CancellationToken cancellationToken)
        {
            var trips = await _context.Trips.IgnoreQueryFilters().Include(t => t.Customer)
                .Where(t => tripIds.Contains(t.Id)).ToListAsync(cancellationToken);
            if (trips.Count == 0) return;

            var stops = await _context.TripStopNotes.Where(s => tripIds.Contains(s.TripId)).ToListAsync(cancellationToken);
            var cache = await NotesCache.LoadAsync(this, trips, stops, cancellationToken);

            foreach (var trip in trips)
            {
                var owner = cache.OwnerOf(trip);
                var closed = trip.IsCancelled || ClosedStatuses.Contains(trip.Status) || trip.Date.Date < _clock.TodayFor(trip.ProviderId);

                foreach (var side in new[] { TripSide.Pickup, TripSide.Dropoff })
                {
                    var isPickup = side == TripSide.Pickup;
                    var stop = stops.FirstOrDefault(s => s.TripId == trip.Id && s.Side == side);
                    if (stop == null)
                    {
                        // A trip from before RE-054: its comment is adopted as what the source said,
                        // so the Rider app and the integration API keep receiving exactly what they did.
                        stop = new TripStopNote { Trip = trip, Side = side, FileComment = Clean(isPickup ? trip.PickupComment : trip.DropoffComment) };
                        _context.TripStopNotes.Add(stop);
                        stops.Add(stop);
                    }

                    if (!stop.PlaceChosenByUser || stop.PlaceId == null)
                    {
                        var (place, entrance) = await cache.ResolveAsync(owner, trip.Customer,
                            isPickup ? trip.PickupAddress : trip.DropoffAddress, isPickup ? trip.PickupCity : trip.DropoffCity,
                            isPickup ? trip.PickupLatitude : trip.DropoffLatitude, isPickup ? trip.PickupLongitude : trip.DropoffLongitude,
                            create: true, cancellationToken);
                        stop.Place = place;
                        stop.PlaceEntrance = entrance;
                        if (place == null) stop.PlaceId = null;
                        if (entrance == null) stop.PlaceEntranceId = null;
                    }

                    var stopPlace = stop.Place ?? cache.PlaceById(stop.PlaceId);
                    if (stopPlace != null) cache.Learn(stopPlace, owner, trip.CustomerId, stop.FileComment);

                    var pickupDone = isPickup && trip.Status == TripStatus.InProgress;
                    if (work == NotesWork.Compose && !closed && !pickupDone)
                    {
                        var current = isPickup ? trip.PickupComment : trip.DropoffComment;
                        if (stop.ComposedHash == null || StableHash(current) != stop.ComposedHash)
                            stop.TripNote = NoteComposer.TripNoteFromEditedComment(current, cache.Sources(stop, stopPlace, trip.CustomerId, owner, includeTripNote: false));

                        var composed = NoteComposer.Compose(cache.Sources(stop, stopPlace, trip.CustomerId, owner, includeTripNote: true));
                        if (isPickup) trip.PickupComment = composed;
                        else trip.DropoffComment = composed;
                        stop.ComposedHash = StableHash(composed);
                    }
                    stop.ProcessedAtUtc = _clock.UtcNow;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Places and notes for one batch, loaded once and searched in memory. Only a stop that
        /// matches nothing by address asks the database again (an entrance nearby, a catalog name).
        /// </summary>
        private sealed class NotesCache
        {
            private readonly TripNotesService _s;
            private readonly HashSet<int> _tenants;
            private readonly List<Place> _places;
            private readonly Dictionary<Place, List<PlaceNote>> _placeNotes;
            private readonly List<CustomerNote> _customerNotes;

            private NotesCache(TripNotesService s, HashSet<int> tenants, List<Place> places, List<PlaceNote> placeNotes, List<CustomerNote> customerNotes)
            {
                _s = s;
                _tenants = tenants;
                _places = places;
                _placeNotes = places.ToDictionary(p => p, p => placeNotes.Where(n => n.PlaceId == p.Id).OrderBy(n => n.Id).ToList());
                _customerNotes = customerNotes;
            }

            public static async Task<NotesCache> LoadAsync(TripNotesService s, List<Trip> trips, List<TripStopNote> stops, CancellationToken ct)
            {
                var context = s._context;
                var tenants = (await context.Providers.IgnoreQueryFilters().Where(p => p.IsTenant).Select(p => p.Id).ToListAsync(ct)).ToHashSet();

                var keys = trips.SelectMany(t => new[] { NoteText.AddressKey(t.PickupAddress), NoteText.AddressKey(t.DropoffAddress) })
                    .Where(k => k.Length > 0).Distinct().ToList();
                var customerIds = trips.Select(t => t.CustomerId).Distinct().ToList();
                var linked = stops.Where(x => x.PlaceId != null).Select(x => x.PlaceId!.Value).Distinct().ToList();

                var places = await context.Places.IgnoreQueryFilters().Include(p => p.Entrances)
                    .Where(p => (p.IsActive && (keys.Contains(p.AddressKey) || (p.CustomerId != null && customerIds.Contains(p.CustomerId.Value))))
                                || linked.Contains(p.Id))
                    .ToListAsync(ct);
                var placeIds = places.Select(p => p.Id).ToList();
                var placeNotes = await context.PlaceNotes.Where(n => placeIds.Contains(n.PlaceId)).ToListAsync(ct);
                var customerNotes = await context.CustomerNotes.IgnoreQueryFilters()
                    .Where(n => customerIds.Contains(n.CustomerId)).OrderBy(n => n.Id).ToListAsync(ct);

                return new NotesCache(s, tenants, places, placeNotes, customerNotes);
            }

            /// <summary>The company the trip's places and notes belong to: its tenant, or Raphael (null).</summary>
            public int? OwnerOf(Trip trip) =>
                trip.ProviderId is int provider && _tenants.Contains(provider) ? provider : null;

            public Place? PlaceById(int? id) => id == null ? null : _places.FirstOrDefault(p => p.Id == id);

            public async Task<(Place? Place, PlaceEntrance? Entrance)> ResolveAsync(int? owner, Customer? customer,
                string? address, string? city, double lat, double lng, bool create, CancellationToken ct)
            {
                if (string.IsNullOrWhiteSpace(address)) return (null, null);

                var key = NoteText.AddressKey(address);
                var hasPoint = lat != 0 && lng != 0;
                var mine = _places.Where(p => p.IsActive && p.OwnerProviderId == owner).ToList();

                // 1. One of this patient's homes: same address, or a stop at its door.
                Place? match = null;
                if (customer != null)
                {
                    var homes = mine.Where(p => p.CustomerId == customer.Id).ToList();
                    match = homes.FirstOrDefault(p => p.AddressKey == key)
                        ?? (hasPoint ? homes.FirstOrDefault(p => p.Entrances.Any(e => e.IsActive && Meters(e, lat, lng) <= HomeMeters)) : null);
                }

                // 2. A facility at this address.
                match ??= mine.FirstOrDefault(p => p.CustomerId == null && p.AddressKey == key);

                // 3. A facility with an entrance right where this stop is: in memory first, then the database.
                if (match == null && hasPoint)
                {
                    match = Nearest(mine.Where(p => p.CustomerId == null), lat, lng);
                    if (match == null)
                    {
                        var dLat = SameEntranceMeters / 111_000d;
                        var dLng = dLat / Math.Max(0.2, Math.Cos(lat * Math.PI / 180));
                        var near = await _s._context.Places.IgnoreQueryFilters().Include(p => p.Entrances)
                            .Where(p => p.CustomerId == null && p.IsActive && p.OwnerProviderId == owner && p.Entrances.Any(e =>
                                e.IsActive && e.Latitude >= lat - dLat && e.Latitude <= lat + dLat
                                && e.Longitude >= lng - dLng && e.Longitude <= lng + dLng))
                            .ToListAsync(ct);
                        var fresh = near.Where(n => !_places.Contains(n)).ToList();
                        if (fresh.Count > 0)
                        {
                            var freshIds = fresh.Select(p => p.Id).ToList();
                            var freshNotes = await _s._context.PlaceNotes.Where(n => freshIds.Contains(n.PlaceId)).ToListAsync(ct);
                            foreach (var place in fresh)
                            {
                                _places.Add(place);
                                _placeNotes[place] = freshNotes.Where(n => n.PlaceId == place.Id).OrderBy(n => n.Id).ToList();
                            }
                        }
                        match = Nearest(near, lat, lng);
                    }
                }

                if (match == null && !create) return (null, null);

                // 4. New place: the patient's home if it is the address on their record, a facility otherwise.
                if (match == null)
                {
                    var isHome = customer != null && (
                        NoteText.SameStreetAddress(key, NoteText.AddressKey(customer.Address))
                        || (hasPoint && customer.Latitude is double cLat && customer.Longitude is double cLng
                            && RemainingDistance.HaversineMeters(lat, lng, cLat, cLng) <= HomeMeters));

                    match = new Place
                    {
                        Kind = isHome ? PlaceKind.Home : PlaceKind.Facility,
                        Address = Truncate(address.Trim(), 450),
                        City = string.IsNullOrWhiteSpace(city) ? null : Truncate(city.Trim(), 100),
                        AddressKey = key,
                        CustomerId = isHome ? customer!.Id : null,
                        OwnerProviderId = owner,
                        Source = NoteSource.Import,
                        CreatedAtUtc = _s._clock.UtcNow,
                        CreatedByUserId = _s._currentUser.UserId,
                    };

                    if (isHome)
                    {
                        match.Name = Truncate($"{customer!.FullName} Home", 200);
                    }
                    else
                    {
                        // The catalog's name when a catalog entry is right here; the address otherwise.
                        // Never the file's "place name": in the trip files it carries the patient's name.
                        var catalog = hasPoint ? await NearestCatalogAsync(lat, lng, ct) : null;
                        match.Name = Truncate(catalog?.Name ?? address.Trim(), 200);
                        match.CatalogIntegratorId = catalog?.Id;
                    }

                    _s._context.Places.Add(match);
                    Add(match);
                }

                if (!hasPoint) return (match, null);

                var entrance = match.Entrances
                    .Where(e => e.IsActive)
                    .Select(e => (Entrance: e, Distance: Meters(e, lat, lng)))
                    .Where(x => x.Distance <= SameEntranceMeters)
                    .OrderBy(x => x.Distance).Select(x => x.Entrance).FirstOrDefault();

                if (entrance == null && create)
                {
                    entrance = new PlaceEntrance { Place = match, Latitude = lat, Longitude = lng, CreatedAtUtc = _s._clock.UtcNow };
                    _s._context.PlaceEntrances.Add(entrance); // EF puts it in match.Entrances too
                }
                return (match, entrance);
            }

            private async Task<CatalogIntegrator?> NearestCatalogAsync(double lat, double lng, CancellationToken ct)
            {
                var dLat = CatalogMeters / 111_000d;
                var dLng = dLat / Math.Max(0.2, Math.Cos(lat * Math.PI / 180));
                var candidates = await _s._context.CatalogIntegrators.IgnoreQueryFilters().AsNoTracking()
                    .Where(c => c.IsActive && c.Latitude != null && c.Longitude != null
                        && c.Latitude >= lat - dLat && c.Latitude <= lat + dLat
                        && c.Longitude >= lng - dLng && c.Longitude <= lng + dLng)
                    .Select(c => new CatalogIntegrator { Id = c.Id, Name = c.Name, Latitude = c.Latitude, Longitude = c.Longitude })
                    .ToListAsync(ct);
                return candidates
                    .Select(c => (Entry: c, Distance: RemainingDistance.HaversineMeters(lat, lng, c.Latitude!.Value, c.Longitude!.Value)))
                    .Where(x => x.Distance <= CatalogMeters)
                    .OrderBy(x => x.Distance).Select(x => x.Entry).FirstOrDefault();
            }

            private static Place? Nearest(IEnumerable<Place> places, double lat, double lng) =>
                places
                    .Select(p => (Place: p, Distance: p.Entrances.Where(e => e.IsActive).Select(e => Meters(e, lat, lng)).DefaultIfEmpty(double.MaxValue).Min()))
                    .Where(x => x.Distance <= SameEntranceMeters)
                    .OrderBy(x => x.Distance).Select(x => x.Place).FirstOrDefault();

            private void Add(Place place)
            {
                _places.Add(place);
                if (!_placeNotes.ContainsKey(place)) _placeNotes[place] = place.Notes.OrderBy(n => n.Id).ToList();
            }

            /// <summary>
            /// The lines of a file comment the place and the patient do not have yet become place notes
            /// to review. Compared with every note, removed ones included: a line the office deleted
            /// does not come back with tomorrow's file.
            /// </summary>
            public void Learn(Place place, int? owner, int customerId, string? fileComment)
            {
                if (string.IsNullOrWhiteSpace(fileComment)) return;
                if (!_placeNotes.TryGetValue(place, out var notes)) _placeNotes[place] = notes = new List<PlaceNote>();

                var known = notes.Select(n => n.Text)
                    .Concat(_customerNotes.Where(n => n.CustomerId == customerId && n.OwnerProviderId == owner).Select(n => n.Text));
                foreach (var line in NoteText.NewLines(fileComment, known))
                {
                    var note = new PlaceNote { Place = place, Text = line, Source = NoteSource.Import, Status = NoteStatus.ToReview };
                    _s.Stamp(note);
                    _s._context.PlaceNotes.Add(note);
                    notes.Add(note);
                }
            }

            public StopNoteSources Sources(TripStopNote stop, Place? place, int customerId, int? owner, bool includeTripNote)
            {
                var placeNotes = place != null && _placeNotes.TryGetValue(place, out var list) ? list : new List<PlaceNote>();
                var customerNotes = _customerNotes
                    .Where(n => n.CustomerId == customerId && n.OwnerProviderId == owner && n.Status != NoteStatus.Removed)
                    .Select(n => n.Text).ToList();
                return BuildSources(stop, placeNotes, customerNotes, includeTripNote);
            }
        }

        private static double Meters(PlaceEntrance e, double lat, double lng) =>
            RemainingDistance.HaversineMeters(e.Latitude, e.Longitude, lat, lng);

        private static StopNoteSources BuildSources(TripStopNote stop, IEnumerable<PlaceNote> placeNotes, IReadOnlyList<string> customerNotes, bool includeTripNote)
        {
            var active = placeNotes.Where(n => n.Status != NoteStatus.Removed).OrderBy(n => n.Status).ThenBy(n => n.Id).ToList();
            bool AtThisEntrance(PlaceNote n) =>
                (n.PlaceEntrance != null && stop.PlaceEntrance != null && ReferenceEquals(n.PlaceEntrance, stop.PlaceEntrance))
                || (n.PlaceEntranceId != null && n.PlaceEntranceId == (stop.PlaceEntrance?.Id ?? stop.PlaceEntranceId));
            return new StopNoteSources(
                includeTripNote ? stop.TripNote : null,
                active.Where(n => (n.PlaceEntranceId != null || n.PlaceEntrance != null) && AtThisEntrance(n)).Select(n => n.Text).ToList(),
                active.Where(n => n.PlaceEntranceId == null && n.PlaceEntrance == null).Select(n => n.Text).ToList(),
                customerNotes,
                stop.FileComment);
        }

        /// <summary>A fingerprint that does not change between runs, unlike <see cref="string.GetHashCode()"/>.</summary>
        public static int StableHash(string? text)
        {
            unchecked
            {
                var hash = (int)2166136261;
                foreach (var c in text ?? string.Empty) hash = (hash ^ c) * 16777619;
                return hash;
            }
        }

        // =====================================================================================
        // The notes window (Desktop) and the notes page (Driver)
        // =====================================================================================

        /// <summary>The trip, if the caller may see it. A driver only reaches the trips of their own routes.</summary>
        private async Task<Trip?> LoadTripAsync(int tripId)
        {
            var trip = await _context.Trips.Include(t => t.Customer).Include(t => t.Run).FirstOrDefaultAsync(t => t.Id == tripId);
            if (trip == null) return null;
            if (_currentUser.RoleId == DriverRoleId && (trip.Run == null || trip.Run.DriverId != _currentUser.UserId)) return null;
            return trip;
        }

        /// <summary>
        /// The trip and its two ends, worked now if the worker has not got to it yet: whoever opens the
        /// notes never sees a half-done state.
        /// </summary>
        private async Task<(Trip Trip, List<TripStopNote> Stops)?> LoadWithStopsAsync(int tripId, bool forceWork = false)
        {
            var trip = await LoadTripAsync(tripId);
            if (trip == null) return null;

            var stops = await StopsAsync(tripId);
            var stale = stops.Count < 2 || stops.Any(s => s.ProcessedAtUtc == null)
                || stops.Any(s => s.ComposedHash != null
                    && StableHash(s.Side == TripSide.Pickup ? trip.PickupComment : trip.DropoffComment) != s.ComposedHash);
            if (forceWork || stale)
            {
                await ProcessBatchAsync(new[] { tripId }, NotesWork.Compose, CancellationToken.None);
                stops = await StopsAsync(tripId);
            }
            return (trip, stops);
        }

        private Task<List<TripStopNote>> StopsAsync(int tripId) =>
            _context.TripStopNotes
                .Include(s => s.Place).ThenInclude(p => p!.Entrances)
                .Include(s => s.PlaceEntrance)
                .Where(s => s.TripId == tripId).ToListAsync();

        public async Task<TripNotesDto?> GetAsync(int tripId)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            return loaded == null ? null : await ToDtoAsync(loaded.Value.Trip, loaded.Value.Stops);
        }

        public async Task<TripNotesDto?> SetTripNoteAsync(int tripId, TripSide side, string? text)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            _guard.EnsureTripWritable(trip, "notes");

            var stop = StopOf(stops, side);
            var clean = Clean(text);
            stop.TripNote = clean == null ? null : Truncate(clean, NoteRules.MaxTripNoteLength);
            await _context.SaveChangesAsync();
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> AddPlaceNoteAsync(int tripId, TripSide side, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (_, stops) = loaded.Value;
            var stop = StopOf(stops, side);
            var place = stop.Place ?? throw new NotesValidationException("This end of the trip has no address, so it has no place to keep notes on.");

            var note = new PlaceNote
            {
                Place = place,
                PlaceEntranceId = dto.ForEntrance ? stop.PlaceEntranceId : null,
                Text = ValidText(dto.Text),
                Source = SourceOfCaller(),
                Status = ParseStatus(dto.Status),
            };
            Stamp(note);
            _context.PlaceNotes.Add(note);
            await _context.SaveChangesAsync();

            await QueueUpcomingAsync(place.Id, null, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> AddCustomerNoteAsync(int tripId, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, _) = loaded.Value;

            var note = new CustomerNote
            {
                CustomerId = trip.CustomerId,
                OwnerProviderId = await OwnerOfAsync(trip),
                Text = ValidText(dto.Text),
                Source = SourceOfCaller(),
                Status = ParseStatus(dto.Status),
            };
            Stamp(note);
            _context.CustomerNotes.Add(note);
            await _context.SaveChangesAsync();

            await QueueUpcomingAsync(null, trip.CustomerId, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> UpdatePlaceNoteAsync(int tripId, int noteId, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (_, stops) = loaded.Value;
            var note = await PlaceNoteOfTripAsync(stops, noteId);
            if (note == null) return null;

            note.Text = ValidText(dto.Text);
            note.Status = ParseStatus(dto.Status);
            note.PlaceEntranceId = dto.ForEntrance
                ? stops.FirstOrDefault(s => s.PlaceId == note.PlaceId)?.PlaceEntranceId ?? note.PlaceEntranceId
                : null;
            Touch(note);
            await _context.SaveChangesAsync();

            await QueueUpcomingAsync(note.PlaceId, null, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> UpdateCustomerNoteAsync(int tripId, int noteId, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, _) = loaded.Value;
            var note = await CustomerNoteOfTripAsync(trip, noteId);
            if (note == null) return null;

            note.Text = ValidText(dto.Text);
            note.Status = ParseStatus(dto.Status);
            Touch(note);
            await _context.SaveChangesAsync();

            await QueueUpcomingAsync(null, trip.CustomerId, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> RemovePlaceNoteAsync(int tripId, int noteId)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var note = await PlaceNoteOfTripAsync(loaded.Value.Stops, noteId);
            if (note == null) return null;

            note.Status = NoteStatus.Removed;
            Touch(note);
            await _context.SaveChangesAsync();

            await QueueUpcomingAsync(note.PlaceId, null, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> RemoveCustomerNoteAsync(int tripId, int noteId)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var trip = loaded.Value.Trip;
            var note = await CustomerNoteOfTripAsync(trip, noteId);
            if (note == null) return null;

            note.Status = NoteStatus.Removed;
            Touch(note);
            await _context.SaveChangesAsync();

            await QueueUpcomingAsync(null, trip.CustomerId, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> PromoteTripNoteAsync(int tripId, TripSide side, PromoteTripNoteDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var stop = StopOf(stops, side);
            if (string.IsNullOrWhiteSpace(stop.TripNote)) throw new NotesValidationException("This end of the trip has no note of its own to keep.");

            var toCustomer = string.Equals(dto.Target, "Customer", StringComparison.OrdinalIgnoreCase);
            if (!toCustomer && !string.Equals(dto.Target, "Place", StringComparison.OrdinalIgnoreCase))
                throw new NotesValidationException("Target must be Place or Customer.");
            if (!toCustomer && stop.Place == null)
                throw new NotesValidationException("This end of the trip has no address, so it has no place to keep notes on.");

            var lines = string.IsNullOrWhiteSpace(dto.Text) ? NoteText.SplitLines(stop.TripNote) : new List<string> { dto.Text.Trim() };
            var owner = await OwnerOfAsync(trip);
            foreach (var line in lines)
            {
                var text = ValidText(line);
                if (toCustomer)
                {
                    var note = new CustomerNote { CustomerId = trip.CustomerId, OwnerProviderId = owner, Text = text, Source = SourceOfCaller(), Status = NoteStatus.Active };
                    Stamp(note);
                    _context.CustomerNotes.Add(note);
                }
                else
                {
                    var note = new PlaceNote { Place = stop.Place!, Text = text, Source = SourceOfCaller(), Status = NoteStatus.Active };
                    Stamp(note);
                    _context.PlaceNotes.Add(note);
                }
            }

            // What was kept leaves the trip's own note: it now comes from the place or the patient.
            if (!_guard.IsReadOnly(trip))
            {
                var rest = NoteText.SplitLines(stop.TripNote).Where(l => !NoteText.IsKnown(l, lines)).ToList();
                stop.TripNote = rest.Count == 0 ? null : string.Join(NoteRules.Separator, rest);
            }
            await _context.SaveChangesAsync();

            if (toCustomer) await QueueUpcomingAsync(null, trip.CustomerId, tripId);
            else await QueueUpcomingAsync(stop.Place!.Id, null, tripId);
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> LinkPlaceAsync(int tripId, TripSide side, int placeId)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            _guard.EnsureTripWritable(trip, "notes");

            var place = await _context.Places.Include(p => p.Entrances).FirstOrDefaultAsync(p => p.Id == placeId && p.IsActive)
                ?? throw new NotesValidationException("That place does not exist.");
            if (place.CustomerId != null && place.CustomerId != trip.CustomerId)
                throw new NotesValidationException("That place is another patient's home.");

            var stop = StopOf(stops, side);
            var isPickup = side == TripSide.Pickup;
            var lat = isPickup ? trip.PickupLatitude : trip.DropoffLatitude;
            var lng = isPickup ? trip.PickupLongitude : trip.DropoffLongitude;

            stop.Place = place;
            stop.PlaceChosenByUser = true;
            stop.PlaceEntrance = null;
            stop.PlaceEntranceId = null;
            if (lat != 0 && lng != 0)
            {
                var entrance = place.Entrances.Where(e => e.IsActive)
                    .OrderBy(e => Meters(e, lat, lng)).FirstOrDefault(e => Meters(e, lat, lng) <= SameEntranceMeters);
                if (entrance == null)
                {
                    entrance = new PlaceEntrance { Place = place, Latitude = lat, Longitude = lng, CreatedAtUtc = _clock.UtcNow };
                    _context.PlaceEntrances.Add(entrance);
                }
                stop.PlaceEntrance = entrance;
            }
            await _context.SaveChangesAsync();
            return await RecomposeAsync(tripId);
        }

        public async Task<TripNotesDto?> UpdatePlaceAsync(int tripId, TripSide side, PlaceUpdateDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var stop = StopOf(stops, side);
            var place = stop.Place ?? throw new NotesValidationException("This end of the trip has no place.");

            if (!string.IsNullOrWhiteSpace(dto.Name)) place.Name = Truncate(dto.Name.Trim(), 200);
            if (!string.IsNullOrWhiteSpace(dto.Kind))
            {
                if (!Enum.TryParse<PlaceKind>(dto.Kind, true, out var kind)) throw new NotesValidationException("Kind must be Home or Facility.");
                place.Kind = kind;
                place.CustomerId = kind == PlaceKind.Home ? trip.CustomerId : null;
            }
            if (dto.EntranceLabel != null && stop.PlaceEntrance != null)
                stop.PlaceEntrance.Label = string.IsNullOrWhiteSpace(dto.EntranceLabel) ? null : Truncate(dto.EntranceLabel.Trim(), 100);

            place.UpdatedAtUtc = _clock.UtcNow;
            place.UpdatedByUserId = _currentUser.UserId;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                throw new NotesValidationException("There is already a place like that at this address. Use \"use another place\" to pick it.");
            }
            return await ToDtoAsync(trip, stops);
        }

        public async Task<List<PlaceSummaryDto>> SearchPlacesAsync(string? search, int take)
        {
            take = Math.Clamp(take, 1, 50);
            var query = _context.Places.Where(p => p.IsActive && p.Kind == PlaceKind.Facility);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var key = NoteText.AddressKey(s);
                query = query.Where(p => p.Name.Contains(s) || p.AddressKey.Contains(key));
            }
            return await query.OrderBy(p => p.Name).Take(take).Select(p => new PlaceSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Kind = p.Kind.ToString(),
                Address = p.Address,
                City = p.City,
                EntranceCount = p.Entrances.Count(e => e.IsActive),
            }).ToListAsync();
        }

        public async Task<NotesPreviewDto> PreviewAsync(NotesPreviewRequestDto request)
        {
            var customer = await _context.Customers.FindAsync(request.CustomerId);
            var owner = _currentUser.TenantId;
            var probe = new Trip
            {
                CustomerId = request.CustomerId,
                Customer = customer!,
                PickupAddress = request.PickupAddress ?? string.Empty,
                DropoffAddress = request.DropoffAddress ?? string.Empty,
            };
            var cache = await NotesCache.LoadAsync(this, new List<Trip> { probe }, new List<TripStopNote>(), CancellationToken.None);

            async Task<(string? Comment, string? Name)> SideAsync(string? address, double lat, double lng)
            {
                var (place, entrance) = await cache.ResolveAsync(owner, customer, address, null, lat, lng, create: false, CancellationToken.None);
                var stop = new TripStopNote { PlaceId = place?.Id, PlaceEntranceId = entrance?.Id };
                return (NoteComposer.Compose(cache.Sources(stop, place, request.CustomerId, owner, includeTripNote: false)), place?.Name);
            }

            var pickup = await SideAsync(request.PickupAddress, request.PickupLatitude, request.PickupLongitude);
            var dropoff = await SideAsync(request.DropoffAddress, request.DropoffLatitude, request.DropoffLongitude);
            return new NotesPreviewDto
            {
                PickupComment = pickup.Comment,
                PickupPlaceName = pickup.Name,
                DropoffComment = dropoff.Comment,
                DropoffPlaceName = dropoff.Name,
            };
        }

        public async Task<Dictionary<int, int>> CountNotesToReviewAsync(IReadOnlyCollection<int> tripIds)
        {
            if (tripIds.Count == 0) return new Dictionary<int, int>();

            var stops = await _context.TripStopNotes
                .Where(s => tripIds.Contains(s.TripId) && s.PlaceId != null)
                .Select(s => new { s.TripId, s.PlaceId }).ToListAsync();
            var placeIds = stops.Select(s => s.PlaceId!.Value).Distinct().ToList();
            var byPlace = await _context.PlaceNotes
                .Where(n => placeIds.Contains(n.PlaceId) && n.Status == NoteStatus.ToReview)
                .GroupBy(n => n.PlaceId).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var customers = await _context.Trips.Where(t => tripIds.Contains(t.Id)).Select(t => new { t.Id, t.CustomerId }).ToListAsync();
            var customerIds = customers.Select(c => c.CustomerId).Distinct().ToList();
            var byCustomer = await _context.CustomerNotes
                .Where(n => customerIds.Contains(n.CustomerId) && n.Status == NoteStatus.ToReview)
                .GroupBy(n => n.CustomerId).Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            var result = new Dictionary<int, int>();
            foreach (var trip in customers)
            {
                var count = byCustomer.GetValueOrDefault(trip.CustomerId)
                    + stops.Where(s => s.TripId == trip.Id).Select(s => s.PlaceId!.Value).Distinct().Sum(p => byPlace.GetValueOrDefault(p));
                if (count > 0) result[trip.Id] = count;
            }
            return result;
        }

        public async Task<Dictionary<int, (string? Pickup, string? Dropoff)>> GetExternalCommentsAsync(IEnumerable<Trip> trips)
        {
            var list = trips.ToList();
            var ids = list.Select(t => t.Id).ToList();
            var stops = ids.Count == 0
                ? new List<TripStopNote>()
                : await _context.TripStopNotes.AsNoTracking().Where(s => ids.Contains(s.TripId)).ToListAsync();

            var result = new Dictionary<int, (string?, string?)>();
            foreach (var trip in list)
            {
                var pickup = stops.FirstOrDefault(s => s.TripId == trip.Id && s.Side == TripSide.Pickup);
                var dropoff = stops.FirstOrDefault(s => s.TripId == trip.Id && s.Side == TripSide.Dropoff);
                result[trip.Id] = (
                    pickup != null ? pickup.FileComment : trip.PickupComment,
                    dropoff != null ? dropoff.FileComment : trip.DropoffComment);
            }
            return result;
        }

        public async Task ApplyExternalCommentsAsync(IEnumerable<Raphael.Shared.DTOs.TripReadDto> trips)
        {
            var list = trips.ToList();
            var ids = list.Select(t => t.Id).ToList();
            if (ids.Count == 0) return;
            var stops = await _context.TripStopNotes.AsNoTracking().Where(s => ids.Contains(s.TripId)).ToListAsync();
            foreach (var trip in list)
            {
                var pickup = stops.FirstOrDefault(s => s.TripId == trip.Id && s.Side == TripSide.Pickup);
                var dropoff = stops.FirstOrDefault(s => s.TripId == trip.Id && s.Side == TripSide.Dropoff);
                if (pickup != null) trip.PickupComment = pickup.FileComment;
                if (dropoff != null) trip.DropoffComment = dropoff.FileComment;
            }
        }

        // =====================================================================================
        // Helpers
        // =====================================================================================

        /// <summary>Works this trip again now, and answers with its window.</summary>
        private async Task<TripNotesDto?> RecomposeAsync(int tripId)
        {
            await ProcessBatchAsync(new[] { tripId }, NotesWork.Compose, CancellationToken.None);
            var trip = await LoadTripAsync(tripId);
            return trip == null ? null : await ToDtoAsync(trip, await StopsAsync(tripId));
        }

        /// <summary>
        /// The other trips still to come at this place, or of this patient, are queued for the worker:
        /// the window answers at once, whatever their number.
        /// </summary>
        private async Task QueueUpcomingAsync(int? placeId, int? customerId, int exceptTripId)
        {
            var from = _clock.UtcNow.Date.AddDays(-1);
            var query = _context.Trips.Where(t => t.Id != exceptTripId && !t.IsCancelled && t.Date >= from && !ClosedStatuses.Contains(t.Status));
            if (placeId is int pid)
            {
                var atPlace = _context.TripStopNotes.Where(s => s.PlaceId == pid).Select(s => s.TripId);
                query = query.Where(t => atPlace.Contains(t.Id));
            }
            else if (customerId is int cid)
            {
                query = query.Where(t => t.CustomerId == cid);
            }
            else return;

            _queue.Enqueue(await query.Select(t => t.Id).ToListAsync(), NotesWork.Compose);
        }

        private async Task<int?> OwnerOfAsync(Trip trip) =>
            trip.ProviderId is int provider
            && await _context.Providers.IgnoreQueryFilters().AnyAsync(p => p.Id == provider && p.IsTenant)
                ? provider : null;

        private static TripStopNote StopOf(List<TripStopNote> stops, TripSide side) =>
            stops.FirstOrDefault(s => s.Side == side)
            ?? throw new NotesValidationException("This trip's notes are not ready yet. Try again in a moment.");

        private async Task<PlaceNote?> PlaceNoteOfTripAsync(List<TripStopNote> stops, int noteId)
        {
            var placeIds = stops.Where(s => s.PlaceId != null).Select(s => s.PlaceId!.Value).ToList();
            return await _context.PlaceNotes.FirstOrDefaultAsync(n => n.Id == noteId && placeIds.Contains(n.PlaceId) && n.Status != NoteStatus.Removed);
        }

        private async Task<CustomerNote?> CustomerNoteOfTripAsync(Trip trip, int noteId)
        {
            var owner = await OwnerOfAsync(trip);
            return await _context.CustomerNotes.IgnoreQueryFilters()
                .FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == trip.CustomerId && n.OwnerProviderId == owner && n.Status != NoteStatus.Removed);
        }

        private async Task<TripNotesDto> ToDtoAsync(Trip trip, List<TripStopNote> stops)
        {
            var owner = await OwnerOfAsync(trip);
            var placeIds = stops.Where(s => s.PlaceId != null).Select(s => s.PlaceId!.Value).Distinct().ToList();
            var placeNotes = await _context.PlaceNotes
                .Where(n => placeIds.Contains(n.PlaceId) && n.Status != NoteStatus.Removed)
                .OrderBy(n => n.Status).ThenBy(n => n.Id).ToListAsync();
            var customerNotes = await _context.CustomerNotes.IgnoreQueryFilters()
                .Where(n => n.CustomerId == trip.CustomerId && n.OwnerProviderId == owner && n.Status != NoteStatus.Removed)
                .OrderBy(n => n.Status).ThenBy(n => n.Id).ToListAsync();

            TripStopNotesDto Side(TripSide side)
            {
                var stop = stops.FirstOrDefault(s => s.Side == side);
                var dto = new TripStopNotesDto
                {
                    Side = side.ToString(),
                    Composed = side == TripSide.Pickup ? trip.PickupComment : trip.DropoffComment,
                };
                if (stop == null) return dto;

                dto.FileComment = stop.FileComment;
                dto.TripNote = stop.TripNote;
                if (stop.Place != null)
                {
                    dto.Place = new PlaceSummaryDto
                    {
                        Id = stop.Place.Id,
                        Name = stop.Place.Name,
                        Kind = stop.Place.Kind.ToString(),
                        Address = stop.Place.Address,
                        City = stop.Place.City,
                        EntranceId = stop.PlaceEntranceId,
                        EntranceLabel = stop.PlaceEntrance?.Label,
                        EntranceCount = stop.Place.Entrances.Count(e => e.IsActive),
                    };
                    var mine = placeNotes.Where(n => n.PlaceId == stop.PlaceId).ToList();
                    dto.EntranceNotes = mine.Where(n => n.PlaceEntranceId != null && n.PlaceEntranceId == stop.PlaceEntranceId).Select(ToDto).ToList();
                    dto.PlaceNotes = mine.Where(n => n.PlaceEntranceId == null).Select(ToDto).ToList();
                }
                return dto;
            }

            return new TripNotesDto
            {
                TripId = trip.Id,
                CustomerId = trip.CustomerId,
                CustomerName = trip.Customer?.FullName,
                TripReadOnly = _guard.IsReadOnly(trip),
                Pickup = Side(TripSide.Pickup),
                Dropoff = Side(TripSide.Dropoff),
                CustomerNotes = customerNotes.Select(ToDto).ToList(),
                NotesToReview = placeNotes.Count(n => n.Status == NoteStatus.ToReview)
                    + customerNotes.Count(n => n.Status == NoteStatus.ToReview),
            };
        }

        private static NoteDto ToDto(NoteBase n) => new()
        {
            Id = n.Id,
            Text = n.Text,
            Source = n.Source.ToString(),
            Status = n.Status.ToString(),
            EntranceId = (n as PlaceNote)?.PlaceEntranceId,
            CreatedBy = n.CreatedByName,
            CreatedAtUtc = n.CreatedAtUtc,
            UpdatedBy = n.UpdatedByName,
            UpdatedAtUtc = n.UpdatedAtUtc,
        };

        private NoteSource SourceOfCaller() =>
            _currentUser.RoleId == DriverRoleId ? NoteSource.Driver
            : _currentUser.UserId == null ? NoteSource.Import
            : NoteSource.Dispatcher;

        private void Stamp(NoteBase note)
        {
            note.CreatedAtUtc = _clock.UtcNow;
            note.CreatedByUserId = _currentUser.UserId;
            note.CreatedByName = Truncate(_currentUser.UserName ?? "System", 100);
        }

        private void Touch(NoteBase note)
        {
            note.UpdatedAtUtc = _clock.UtcNow;
            note.UpdatedByUserId = _currentUser.UserId;
            note.UpdatedByName = Truncate(_currentUser.UserName ?? "System", 100);
        }

        private static NoteStatus ParseStatus(string? status) =>
            string.IsNullOrWhiteSpace(status) ? NoteStatus.Active
            : Enum.TryParse<NoteStatus>(status, true, out var parsed) && parsed != NoteStatus.Removed ? parsed
            : throw new NotesValidationException("Status must be Active or ToReview.");

        private static string ValidText(string? text)
        {
            var clean = Clean(text) ?? throw new NotesValidationException("A note needs some text.");
            if (clean.Length > NoteRules.MaxNoteLength)
                throw new NotesValidationException($"A note is at most {NoteRules.MaxNoteLength} characters. Split it into several.");
            return clean;
        }

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max];
    }

    /// <summary>A notes request the API refuses with 400. The message is safe to show: it never carries note text.</summary>
    public sealed class NotesValidationException : Exception
    {
        public NotesValidationException(string message) : base(message) { }
    }
}
