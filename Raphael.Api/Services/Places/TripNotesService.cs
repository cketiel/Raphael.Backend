using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Trips;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Places;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Places;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Routing;
using Raphael.Shared.Time;

namespace Raphael.Api.Services.Places
{
    /// <summary>Who sent the comment text a trip is being saved with.</summary>
    public enum CommentOrigin
    {
        /// <summary>A trip file, a clinic booking or the integration API: the text is theirs, kept as is.</summary>
        External,

        /// <summary>The office, typing in the trip's comment field.</summary>
        Office,
    }

    public interface ITripNotesService
    {
        /// <summary>
        /// Called by every path that saves a trip, before its SaveChanges: finds or creates the places
        /// of both ends, keeps what an external source said, learns its new lines as notes to review,
        /// and writes the composed comment the driver reads. Never calls Google.
        /// </summary>
        Task ApplyIncomingAsync(Trip trip, string? pickupText, string? dropoffText, CommentOrigin origin);

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

        /// <summary>
        /// The one-off seed from the trip history: places and entrances from the trips of a date range,
        /// and the lines of their comments as notes to review. Run month by month; safe to run twice.
        /// With <paramref name="dryRun"/> nothing is written and the result says what would be.
        /// </summary>
        Task<PlacesSeedResultDto> SeedFromHistoryAsync(DateTime from, DateTime to, bool dryRun);
    }

    /// <summary>Notes of places and patients, and the comment each trip end carries for the driver (RE-054).</summary>
    /// <remarks>
    /// PHI: note texts and home addresses never reach a log line, an exception message or a URL.
    /// </remarks>
    public sealed class TripNotesService : ITripNotesService
    {
        /// <summary>A stop this close to an entrance of a place is at that entrance.</summary>
        public const double SameEntranceMeters = 40;

        /// <summary>A stop this close to a patient's home, or their record's position, is that home.</summary>
        public const double HomeMeters = 60;

        private const int DriverRoleId = 2;

        private static readonly string[] ClosedStatuses =
        {
            TripStatus.Finished, TripStatus.Canceled, TripStatus.Billed, TripStatus.Payed,
        };

        private readonly RaphaelContext _context;
        private readonly ICurrentUserService _currentUser;
        private readonly IOperationClock _clock;
        private readonly ITripChangeGuard _guard;

        public TripNotesService(RaphaelContext context, ICurrentUserService currentUser, IOperationClock clock, ITripChangeGuard guard)
        {
            _context = context;
            _currentUser = currentUser;
            _clock = clock;
            _guard = guard;
        }

        // =====================================================================================
        // Saving a trip
        // =====================================================================================

        public async Task ApplyIncomingAsync(Trip trip, string? pickupText, string? dropoffText, CommentOrigin origin)
        {
            var customer = trip.Customer ?? await _context.Customers.FindAsync(trip.CustomerId);
            var stops = await EnsureStopsAsync(trip);

            foreach (var stop in stops)
            {
                var isPickup = stop.Side == TripSide.Pickup;
                var (place, entrance) = await ResolvePlaceAsync(
                    customer,
                    isPickup ? trip.PickupAddress : trip.DropoffAddress,
                    isPickup ? trip.PickupCity : trip.DropoffCity,
                    isPickup ? trip.PickupLatitude : trip.DropoffLatitude,
                    isPickup ? trip.PickupLongitude : trip.DropoffLongitude,
                    isPickup ? trip.Pickup : trip.Dropoff,
                    create: true);
                stop.Place = place;
                stop.PlaceEntrance = entrance;

                var text = isPickup ? pickupText : dropoffText;
                if (origin == CommentOrigin.External)
                {
                    stop.FileComment = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
                    if (place != null) await LearnFromFileAsync(place, trip.CustomerId, stop.FileComment);
                }
                else
                {
                    var current = isPickup ? trip.PickupComment : trip.DropoffComment;
                    if (!string.Equals(Clean(text), Clean(current), StringComparison.Ordinal))
                    {
                        var sources = await SourcesAsync(stop, trip.CustomerId, includeTripNote: false);
                        stop.TripNote = NoteComposer.TripNoteFromEditedComment(text, sources);
                    }
                }
            }

            await ComposeAsync(trip, stops);
        }

        /// <summary>
        /// The two ends of the trip, created when missing. A trip from before RE-054 has a comment and
        /// no rows: that comment is adopted as what the source said, so the Rider app and the
        /// integration API keep receiving exactly what they did.
        /// </summary>
        private async Task<List<TripStopNote>> EnsureStopsAsync(Trip trip)
        {
            var stops = IsNew(trip)
                ? new List<TripStopNote>()
                : await _context.TripStopNotes.Where(s => s.TripId == trip.Id).ToListAsync();

            foreach (var side in new[] { TripSide.Pickup, TripSide.Dropoff })
            {
                if (stops.Any(s => s.Side == side)) continue;
                var stop = new TripStopNote
                {
                    Trip = trip,
                    Side = side,
                    FileComment = IsNew(trip) ? null : Clean(side == TripSide.Pickup ? trip.PickupComment : trip.DropoffComment),
                };
                _context.TripStopNotes.Add(stop);
                stops.Add(stop);
            }
            return stops.OrderBy(s => s.Side).ToList();
        }

        /// <summary>
        /// The lines of a file comment the place and the patient do not have yet become place notes to
        /// review. Compared with every note, removed ones included: a line the office deleted does not
        /// come back with tomorrow's file.
        /// </summary>
        private async Task LearnFromFileAsync(Place place, int customerId, string? fileComment, NoteSource source = NoteSource.Import)
        {
            if (string.IsNullOrWhiteSpace(fileComment)) return;

            var known = new List<string>();
            if (!IsNew(place))
                known.AddRange(await _context.PlaceNotes.Where(n => n.PlaceId == place.Id).Select(n => n.Text).ToListAsync());
            known.AddRange(place.Notes.Where(IsNew).Select(n => n.Text));
            known.AddRange(await _context.CustomerNotes.Where(n => n.CustomerId == customerId).Select(n => n.Text).ToListAsync());

            foreach (var line in NoteText.NewLines(fileComment, known))
            {
                var note = new PlaceNote { Place = place, Text = line, Source = source, Status = NoteStatus.ToReview };
                Stamp(note);
                _context.PlaceNotes.Add(note); // EF puts it in place.Notes too
            }
        }

        // =====================================================================================
        // Finding a place — SQL only, never Google
        // =====================================================================================

        private async Task<(Place? Place, PlaceEntrance? Entrance)> ResolvePlaceAsync(
            Customer? customer, string? address, string? city, double lat, double lng, string? placeName, bool create)
        {
            if (string.IsNullOrWhiteSpace(address)) return (null, null);

            var key = NoteText.AddressKey(address);
            var hasPoint = lat != 0 && lng != 0;

            // 1. One of this patient's homes: same address, or a stop at its door.
            Place? match = null;
            if (customer != null)
            {
                var homes = await _context.Places.Include(p => p.Entrances)
                    .Where(p => p.CustomerId == customer.Id && p.IsActive).ToListAsync();
                homes.AddRange(_context.Places.Local.Where(p => IsNew(p) && p.CustomerId == customer.Id && p.IsActive));
                match = homes.FirstOrDefault(p => p.AddressKey == key)
                    ?? (hasPoint ? homes.FirstOrDefault(p => p.Entrances.Any(e => e.IsActive && Meters(e, lat, lng) <= HomeMeters)) : null);
            }

            // 2. A facility at this address.
            if (match == null)
            {
                match = _context.Places.Local.FirstOrDefault(p => IsNew(p) && p.CustomerId == null && p.IsActive && p.AddressKey == key)
                    ?? await _context.Places.Include(p => p.Entrances)
                        .Where(p => p.CustomerId == null && p.IsActive && p.AddressKey == key)
                        .OrderBy(p => p.Id).FirstOrDefaultAsync();
            }

            // 3. A facility with an entrance right where this stop is (the file spelled the address another way).
            if (match == null && hasPoint)
            {
                var dLat = SameEntranceMeters / 111_000d;
                var dLng = dLat / Math.Max(0.2, Math.Cos(lat * Math.PI / 180));
                var near = await _context.Places.Include(p => p.Entrances)
                    .Where(p => p.CustomerId == null && p.IsActive && p.Entrances.Any(e =>
                        e.IsActive && e.Latitude >= lat - dLat && e.Latitude <= lat + dLat
                        && e.Longitude >= lng - dLng && e.Longitude <= lng + dLng))
                    .ToListAsync();
                match = near
                    .Select(p => (Place: p, Distance: p.Entrances.Where(e => e.IsActive).Min(e => Meters(e, lat, lng))))
                    .Where(x => x.Distance <= SameEntranceMeters)
                    .OrderBy(x => x.Distance).Select(x => x.Place).FirstOrDefault();
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
                    Name = Truncate(isHome
                        ? $"{customer!.FullName} Home"
                        : (string.IsNullOrWhiteSpace(placeName) ? address.Trim() : placeName.Trim()), 200),
                    Address = Truncate(address.Trim(), 450),
                    City = string.IsNullOrWhiteSpace(city) ? null : Truncate(city.Trim(), 100),
                    AddressKey = key,
                    CustomerId = isHome ? customer!.Id : null,
                    Source = SourceOfCaller(),
                    CreatedAtUtc = _clock.UtcNow,
                    CreatedByUserId = _currentUser.UserId,
                };
                _context.Places.Add(match);
            }

            if (!hasPoint) return (match, null);

            var entrance = match.Entrances
                .Where(e => e.IsActive)
                .Select(e => (Entrance: e, Distance: Meters(e, lat, lng)))
                .Where(x => x.Distance <= SameEntranceMeters)
                .OrderBy(x => x.Distance).Select(x => x.Entrance).FirstOrDefault();

            if (entrance == null && create)
            {
                entrance = new PlaceEntrance { Place = match, Latitude = lat, Longitude = lng, CreatedAtUtc = _clock.UtcNow };
                _context.PlaceEntrances.Add(entrance); // EF puts it in match.Entrances too
            }
            return (match, entrance);
        }

        /// <summary>Added in this save and not stored yet. Not "Id == 0": some providers hand out keys on Add.</summary>
        private bool IsNew(object entity) => _context.Entry(entity).State == EntityState.Added;

        private static double Meters(PlaceEntrance e, double lat, double lng) =>
            RemainingDistance.HaversineMeters(e.Latitude, e.Longitude, lat, lng);

        // =====================================================================================
        // Composing
        // =====================================================================================

        /// <summary>What reaches the driver at one stop, read from the database (and from what is pending in this save).</summary>
        private async Task<StopNoteSources> SourcesAsync(TripStopNote stop, int customerId, bool includeTripNote)
        {
            var placeId = stop.Place != null ? (IsNew(stop.Place) ? (int?)null : stop.Place.Id) : stop.PlaceId;
            var placeNotes = new List<PlaceNote>();
            if (placeId is int id)
                placeNotes.AddRange(await _context.PlaceNotes.Where(n => n.PlaceId == id && n.Status != NoteStatus.Removed).ToListAsync());
            if (stop.Place != null)
                placeNotes.AddRange(stop.Place.Notes.Where(n => IsNew(n) && n.Status != NoteStatus.Removed));

            var customerNotes = await _context.CustomerNotes
                .Where(n => n.CustomerId == customerId && n.Status != NoteStatus.Removed)
                .OrderBy(n => n.Id).Select(n => n.Text).ToListAsync();

            return BuildSources(stop, placeNotes, customerNotes, includeTripNote);
        }

        private static StopNoteSources BuildSources(TripStopNote stop, IEnumerable<PlaceNote> placeNotes, IReadOnlyList<string> customerNotes, bool includeTripNote)
        {
            var entranceId = stop.PlaceEntrance?.Id ?? stop.PlaceEntranceId;
            var ordered = placeNotes.OrderBy(n => n.Status).ThenBy(n => n.Id).ToList();
            return new StopNoteSources(
                includeTripNote ? stop.TripNote : null,
                ordered.Where(n => n.PlaceEntranceId != null && (n.PlaceEntranceId == entranceId || (n.PlaceEntrance != null && n.PlaceEntrance == stop.PlaceEntrance)))
                    .Select(n => n.Text).ToList(),
                ordered.Where(n => n.PlaceEntranceId == null && n.PlaceEntrance == null).Select(n => n.Text).ToList(),
                customerNotes,
                stop.FileComment);
        }

        private async Task ComposeAsync(Trip trip, IEnumerable<TripStopNote> stops)
        {
            foreach (var stop in stops)
            {
                var composed = NoteComposer.Compose(await SourcesAsync(stop, trip.CustomerId, includeTripNote: true));
                if (stop.Side == TripSide.Pickup) trip.PickupComment = composed;
                else trip.DropoffComment = composed;
            }
        }

        /// <summary>
        /// After a place's or a patient's notes change: the trips still to come that stop there get
        /// their comment composed again. Past and closed trips keep what they carried; a pickup already
        /// done keeps its comment. The trip's own notes are never touched.
        /// </summary>
        private async Task RefreshAsync(int? placeId, int? customerId)
        {
            var from = _clock.UtcNow.Date.AddDays(-1);
            var query = _context.Trips.Where(t => !t.IsCancelled && t.Date >= from && !ClosedStatuses.Contains(t.Status));

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

            var trips = (await query.ToListAsync())
                .Where(t => t.Date.Date >= _clock.TodayFor(t.ProviderId))
                .ToList();
            if (trips.Count == 0) return;

            // Everything in a few queries, not a few per trip: a hospital can have hundreds of trips ahead.
            var tripIds = trips.Select(t => t.Id).ToList();
            var allStops = await _context.TripStopNotes.Include(s => s.PlaceEntrance)
                .Where(s => tripIds.Contains(s.TripId)).ToListAsync();

            foreach (var trip in trips.Where(t => allStops.Count(s => s.TripId == t.Id) < 2 || allStops.Any(s => s.TripId == t.Id && s.PlaceId == null)))
            {
                // A trip from before RE-054 learns its places here, without learning notes from its comment.
                var customer = await _context.Customers.FindAsync(trip.CustomerId);
                foreach (var stop in (await EnsureStopsAsync(trip)).Where(s => s.PlaceId == null && s.Place == null))
                {
                    var isPickup = stop.Side == TripSide.Pickup;
                    var (place, entrance) = await ResolvePlaceAsync(customer,
                        isPickup ? trip.PickupAddress : trip.DropoffAddress, isPickup ? trip.PickupCity : trip.DropoffCity,
                        isPickup ? trip.PickupLatitude : trip.DropoffLatitude, isPickup ? trip.PickupLongitude : trip.DropoffLongitude,
                        isPickup ? trip.Pickup : trip.Dropoff, create: true);
                    stop.Place = place;
                    stop.PlaceEntrance = entrance;
                    if (!allStops.Contains(stop)) allStops.Add(stop);
                }
            }

            var placeIds = allStops.Select(s => s.Place != null ? (IsNew(s.Place) ? (int?)null : s.Place.Id) : s.PlaceId).Where(id => id != null).Select(id => id!.Value).Distinct().ToList();
            var placeNotes = (await _context.PlaceNotes
                    .Where(n => placeIds.Contains(n.PlaceId) && n.Status != NoteStatus.Removed).ToListAsync())
                .ToLookup(n => n.PlaceId);
            var customerIds = trips.Select(t => t.CustomerId).Distinct().ToList();
            var customerNotes = (await _context.CustomerNotes
                    .Where(n => customerIds.Contains(n.CustomerId) && n.Status != NoteStatus.Removed)
                    .OrderBy(n => n.Id).ToListAsync())
                .ToLookup(n => n.CustomerId, n => n.Text);

            foreach (var trip in trips)
            {
                var pickupDone = trip.Status == TripStatus.InProgress;
                foreach (var stop in allStops.Where(s => s.TripId == trip.Id || s.Trip == trip))
                {
                    if (pickupDone && stop.Side == TripSide.Pickup) continue;
                    var stopPlaceId = stop.Place != null ? (IsNew(stop.Place) ? -1 : stop.Place.Id) : stop.PlaceId ?? -1;
                    var notes = placeNotes[stopPlaceId].Concat(stop.Place?.Notes.Where(IsNew) ?? Enumerable.Empty<PlaceNote>());
                    var composed = NoteComposer.Compose(BuildSources(stop, notes, customerNotes[trip.CustomerId].ToList(), includeTripNote: true));
                    if (stop.Side == TripSide.Pickup) trip.PickupComment = composed;
                    else trip.DropoffComment = composed;
                }
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

        /// <summary>Loads the trip and its two ends; creates the ends (and finds the places) of a trip from before RE-054.</summary>
        private async Task<(Trip Trip, List<TripStopNote> Stops)?> LoadWithStopsAsync(int tripId)
        {
            var trip = await LoadTripAsync(tripId);
            if (trip == null) return null;

            var stops = await _context.TripStopNotes
                .Include(s => s.Place).ThenInclude(p => p!.Entrances)
                .Include(s => s.PlaceEntrance)
                .Where(s => s.TripId == tripId).ToListAsync();

            if (stops.Count < 2 || stops.Any(s => s.PlaceId == null))
            {
                // First time this trip is opened after RE-054. Writing here is safe: it adopts the
                // comment as the source's text and composes the same lines plus the place's notes.
                if (!_guard.IsReadOnly(trip))
                {
                    // The current comments go back in unchanged, so no trip note is rewritten.
                    await ApplyIncomingAsync(trip, trip.PickupComment, trip.DropoffComment, CommentOrigin.Office);
                    await _context.SaveChangesAsync();
                    stops = await _context.TripStopNotes
                        .Include(s => s.Place).ThenInclude(p => p!.Entrances)
                        .Include(s => s.PlaceEntrance)
                        .Where(s => s.TripId == tripId).ToListAsync();
                }
            }
            return (trip, stops);
        }

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

            var stop = await StopAsync(trip, stops, side);
            var clean = Clean(text);
            stop.TripNote = clean == null ? null : Truncate(clean, NoteRules.MaxTripNoteLength);
            await ComposeAsync(trip, new[] { stop });
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> AddPlaceNoteAsync(int tripId, TripSide side, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var stop = await StopAsync(trip, stops, side);
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

            await RefreshAsync(place.Id, null);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> AddCustomerNoteAsync(int tripId, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;

            var note = new CustomerNote
            {
                CustomerId = trip.CustomerId,
                Text = ValidText(dto.Text),
                Source = SourceOfCaller(),
                Status = ParseStatus(dto.Status),
            };
            Stamp(note);
            _context.CustomerNotes.Add(note);
            await _context.SaveChangesAsync();

            await RefreshAsync(null, trip.CustomerId);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> UpdatePlaceNoteAsync(int tripId, int noteId, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var note = await PlaceNoteOfTripAsync(stops, noteId);
            if (note == null) return null;

            note.Text = ValidText(dto.Text);
            note.Status = ParseStatus(dto.Status);
            if (dto.ForEntrance)
                note.PlaceEntranceId = stops.FirstOrDefault(s => s.PlaceId == note.PlaceId)?.PlaceEntranceId ?? note.PlaceEntranceId;
            else
                note.PlaceEntranceId = null;
            Touch(note);
            await _context.SaveChangesAsync();

            await RefreshAsync(note.PlaceId, null);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> UpdateCustomerNoteAsync(int tripId, int noteId, NoteUpsertDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var note = await _context.CustomerNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == trip.CustomerId && n.Status != NoteStatus.Removed);
            if (note == null) return null;

            note.Text = ValidText(dto.Text);
            note.Status = ParseStatus(dto.Status);
            Touch(note);
            await _context.SaveChangesAsync();

            await RefreshAsync(null, trip.CustomerId);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> RemovePlaceNoteAsync(int tripId, int noteId)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var note = await PlaceNoteOfTripAsync(stops, noteId);
            if (note == null) return null;

            note.Status = NoteStatus.Removed;
            Touch(note);
            await _context.SaveChangesAsync();

            await RefreshAsync(note.PlaceId, null);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> RemoveCustomerNoteAsync(int tripId, int noteId)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var note = await _context.CustomerNotes.FirstOrDefaultAsync(n => n.Id == noteId && n.CustomerId == trip.CustomerId && n.Status != NoteStatus.Removed);
            if (note == null) return null;

            note.Status = NoteStatus.Removed;
            Touch(note);
            await _context.SaveChangesAsync();

            await RefreshAsync(null, trip.CustomerId);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
        }

        public async Task<TripNotesDto?> PromoteTripNoteAsync(int tripId, TripSide side, PromoteTripNoteDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var stop = await StopAsync(trip, stops, side);
            if (string.IsNullOrWhiteSpace(stop.TripNote)) throw new NotesValidationException("This end of the trip has no note of its own to keep.");

            var lines = string.IsNullOrWhiteSpace(dto.Text) ? NoteText.SplitLines(stop.TripNote) : new List<string> { dto.Text.Trim() };
            var toCustomer = string.Equals(dto.Target, "Customer", StringComparison.OrdinalIgnoreCase);
            if (!toCustomer && !string.Equals(dto.Target, "Place", StringComparison.OrdinalIgnoreCase))
                throw new NotesValidationException("Target must be Place or Customer.");
            if (!toCustomer && stop.Place == null)
                throw new NotesValidationException("This end of the trip has no address, so it has no place to keep notes on.");

            foreach (var line in lines)
            {
                var text = ValidText(line);
                if (toCustomer)
                {
                    var note = new CustomerNote { CustomerId = trip.CustomerId, Text = text, Source = SourceOfCaller(), Status = NoteStatus.Active };
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
            var rest = NoteText.SplitLines(stop.TripNote).Where(l => !NoteText.IsKnown(l, lines)).ToList();
            if (!_guard.IsReadOnly(trip))
                stop.TripNote = rest.Count == 0 ? null : string.Join(NoteRules.Separator, rest);
            await _context.SaveChangesAsync();

            if (toCustomer) await RefreshAsync(null, trip.CustomerId);
            else await RefreshAsync(stop.Place!.Id, null);
            if (!_guard.IsReadOnly(trip)) await ComposeAsync(trip, stops);
            await _context.SaveChangesAsync();
            return await ToDtoAsync(trip, stops);
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

            var stop = await StopAsync(trip, stops, side);
            var isPickup = side == TripSide.Pickup;
            var lat = isPickup ? trip.PickupLatitude : trip.DropoffLatitude;
            var lng = isPickup ? trip.PickupLongitude : trip.DropoffLongitude;

            stop.Place = place;
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

            await ComposeAsync(trip, new[] { stop });
            await _context.SaveChangesAsync();
            return await GetAsync(tripId);
        }

        public async Task<TripNotesDto?> UpdatePlaceAsync(int tripId, TripSide side, PlaceUpdateDto dto)
        {
            var loaded = await LoadWithStopsAsync(tripId);
            if (loaded == null) return null;
            var (trip, stops) = loaded.Value;
            var stop = await StopAsync(trip, stops, side);
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
            await _context.SaveChangesAsync();
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
            var customerNotes = customer == null
                ? new List<string>()
                : await _context.CustomerNotes.Where(n => n.CustomerId == customer.Id && n.Status != NoteStatus.Removed)
                    .OrderBy(n => n.Id).Select(n => n.Text).ToListAsync();

            async Task<(string? Comment, string? Name)> SideAsync(string? address, double lat, double lng)
            {
                var (place, entrance) = await ResolvePlaceAsync(customer, address, null, lat, lng, null, create: false);
                var notes = place == null
                    ? new List<PlaceNote>()
                    : await _context.PlaceNotes.Where(n => n.PlaceId == place.Id && n.Status != NoteStatus.Removed).ToListAsync();
                var stop = new TripStopNote { PlaceId = place?.Id, PlaceEntranceId = entrance?.Id };
                return (NoteComposer.Compose(BuildSources(stop, notes, customerNotes, includeTripNote: false)), place?.Name);
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

        public async Task<PlacesSeedResultDto> SeedFromHistoryAsync(DateTime from, DateTime to, bool dryRun)
        {
            const int batchSize = 200;
            var result = new PlacesSeedResultDto { From = from.Date, To = to.Date, DryRun = dryRun };
            var today = _clock.TodayFor(null);

            var ids = await _context.Trips.AsNoTracking()
                .Where(t => t.Date >= from.Date && t.Date <= to.Date)
                .OrderBy(t => t.Date).ThenBy(t => t.Id)
                .Select(t => t.Id).ToListAsync();
            result.Trips = ids.Count;

            for (var i = 0; i < ids.Count; i += batchSize)
            {
                var batch = ids.Skip(i).Take(batchSize).ToList();
                var trips = await _context.Trips.Include(t => t.Customer).Where(t => batch.Contains(t.Id)).ToListAsync();

                foreach (var trip in trips.OrderBy(t => t.Date).ThenBy(t => t.Id))
                {
                    foreach (var side in new[] { TripSide.Pickup, TripSide.Dropoff })
                    {
                        var isPickup = side == TripSide.Pickup;
                        var (place, _) = await ResolvePlaceAsync(trip.Customer,
                            isPickup ? trip.PickupAddress : trip.DropoffAddress, isPickup ? trip.PickupCity : trip.DropoffCity,
                            isPickup ? trip.PickupLatitude : trip.DropoffLatitude, isPickup ? trip.PickupLongitude : trip.DropoffLongitude,
                            isPickup ? trip.Pickup : trip.Dropoff, create: true);
                        if (place != null)
                            await LearnFromFileAsync(place, trip.CustomerId, isPickup ? trip.PickupComment : trip.DropoffComment, NoteSource.History);
                    }

                    // Trips still to come start carrying the place's notes now, not at their next edit.
                    if (!dryRun && trip.Date.Date >= today && !trip.IsCancelled && !ClosedStatuses.Contains(trip.Status))
                    {
                        await ApplyIncomingAsync(trip, trip.PickupComment, trip.DropoffComment, CommentOrigin.Office);
                        result.UpcomingTripsUpdated++;
                    }
                }

                Count(result);
                if (dryRun)
                {
                    // Kept in memory so the next batch still finds what this one "created".
                    continue;
                }
                await _context.SaveChangesAsync();
                _context.ChangeTracker.Clear();
            }

            if (dryRun) _context.ChangeTracker.Clear();
            return result;

            void Count(PlacesSeedResultDto r)
            {
                var added = _context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).Select(e => e.Entity).ToList();
                // In a dry run the tracker keeps growing, so count once at the end of each batch from scratch.
                var homes = added.OfType<Place>().Count(p => p.Kind == PlaceKind.Home);
                var facilities = added.OfType<Place>().Where(p => p.Kind == PlaceKind.Facility).ToList();
                var entrances = added.OfType<PlaceEntrance>().Count();
                var notes = added.OfType<PlaceNote>().Count();
                if (dryRun)
                {
                    r.HomesCreated = homes; r.FacilitiesCreated = facilities.Count; r.EntrancesCreated = entrances; r.NotesCreated = notes;
                    r.FacilitySample = facilities.Select(p => p.Name).Distinct().Take(50).ToList();
                }
                else
                {
                    r.HomesCreated += homes; r.FacilitiesCreated += facilities.Count; r.EntrancesCreated += entrances; r.NotesCreated += notes;
                    foreach (var name in facilities.Select(p => p.Name))
                        if (r.FacilitySample.Count < 50 && !r.FacilitySample.Contains(name)) r.FacilitySample.Add(name);
                }
            }
        }

        // =====================================================================================
        // Helpers
        // =====================================================================================

        private async Task<TripStopNote> StopAsync(Trip trip, List<TripStopNote> stops, TripSide side)
        {
            var stop = stops.FirstOrDefault(s => s.Side == side);
            if (stop != null) return stop;

            // A read-only trip from before RE-054 has no rows and will not get them; its notes window
            // still lets the office write on the place and the patient.
            var created = (await EnsureStopsAsync(trip)).First(s => s.Side == side);
            stops.Add(created);
            return created;
        }

        private async Task<PlaceNote?> PlaceNoteOfTripAsync(List<TripStopNote> stops, int noteId)
        {
            var placeIds = stops.Where(s => s.PlaceId != null).Select(s => s.PlaceId!.Value).ToList();
            return await _context.PlaceNotes.FirstOrDefaultAsync(n => n.Id == noteId && placeIds.Contains(n.PlaceId) && n.Status != NoteStatus.Removed);
        }

        private async Task<TripNotesDto> ToDtoAsync(Trip trip, List<TripStopNote> stops)
        {
            var placeIds = stops.Where(s => s.PlaceId != null).Select(s => s.PlaceId!.Value).Distinct().ToList();
            var placeNotes = await _context.PlaceNotes
                .Where(n => placeIds.Contains(n.PlaceId) && n.Status != NoteStatus.Removed)
                .OrderBy(n => n.Status).ThenBy(n => n.Id).ToListAsync();
            var customerNotes = await _context.CustomerNotes
                .Where(n => n.CustomerId == trip.CustomerId && n.Status != NoteStatus.Removed)
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

            var result = new TripNotesDto
            {
                TripId = trip.Id,
                CustomerId = trip.CustomerId,
                CustomerName = trip.Customer?.FullName,
                TripReadOnly = _guard.IsReadOnly(trip),
                Pickup = Side(TripSide.Pickup),
                Dropoff = Side(TripSide.Dropoff),
                CustomerNotes = customerNotes.Select(ToDto).ToList(),
            };
            result.NotesToReview = placeNotes.Count(n => n.Status == NoteStatus.ToReview)
                + customerNotes.Count(n => n.Status == NoteStatus.ToReview);
            return result;
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
