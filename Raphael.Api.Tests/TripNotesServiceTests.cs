using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Raphael.Api.Services.Places;
using Raphael.Api.Services.Trips;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Places;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Catalog;
using Raphael.Shared.Entities.Places;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;

namespace Raphael.Api.Tests
{
    /// <summary>
    /// Places and notes against a database (EF InMemory), the way they run: an import only keeps the
    /// file's text, and the notes worker does the rest afterwards (RE-054).
    /// </summary>
    public class TripNotesServiceTests
    {
        private static readonly DateTime Today = new(2026, 10, 20);

        private sealed class Office : ICurrentUserService
        {
            public int? UserId { get; init; } = 7;
            public string? UserName { get; init; } = "dispatcher";
            public int? IntegratorId => null;
            public int? ProviderId => null;
            public int? RoleId { get; init; } = 3;
            public bool IsMilanesInternal => true;
            public bool IsExternalProvider => false;
            public bool MustChangePassword => false;
            public int? TenantId => null;
            public bool TenancyEnforced => false;
        }

        private sealed class Clock : IOperationClock
        {
            public DateTime UtcNow => Today.AddHours(12);
            public TimeZoneInfo ZoneFor(int? providerId) => TimeZoneInfo.Utc;
            public DateTime NowFor(int? providerId) => UtcNow;
            public DateTime TodayFor(int? providerId) => Today;
            public TimeSpan TimeOfDayFor(int? providerId) => UtcNow.TimeOfDay;
            public DateTime ToOperation(DateTime utc, int? providerId) => utc;
            public void InvalidateZones() { }
        }

        private sealed class OpenGuard : ITripChangeGuard
        {
            public bool IsReadOnly(Trip trip) => false;
            public bool IsPastDay(DateTime date, int? providerId) => false;
            public void EnsureTripWritable(Trip trip, string operation) { }
            public void EnsureDayWritable(DateTime date, int? providerId, int? tripId, string operation) { }
            public void EnsureRoutedFieldsUnchanged(Trip trip, IReadOnlyList<string> changedFields) { }
        }

        private sealed class RecordingQueue : ITripNotesQueue
        {
            public List<(int TripId, NotesWork Work)> Items { get; } = new();
            public void Enqueue(int tripId, NotesWork work) => Items.Add((tripId, work));
            public void Enqueue(IEnumerable<int> tripIds, NotesWork work) { foreach (var id in tripIds) Enqueue(id, work); }
        }

        private sealed class Fixture
        {
            public RaphaelContext Context { get; }
            public TripNotesService Notes { get; }
            public RecordingQueue Queue { get; } = new();
            public Customer Patient { get; }

            public Fixture(ICurrentUserService? user = null)
            {
                user ??= new Office();
                var options = new DbContextOptionsBuilder<RaphaelContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                Context = new RaphaelContext(options, user);
                Notes = new TripNotesService(Context, user, new Clock(), new OpenGuard(), Queue, NullLogger<TripNotesService>.Instance);

                Patient = NewPatient("Jane Doe", "12 Main Street", 25.7700, -80.1900);
            }

            public Customer NewPatient(string name, string address, double lat, double lng)
            {
                var patient = new Customer
                {
                    FullName = name, Address = address, City = "Miami", State = "FL", Zip = "33101",
                    Gender = "F", CreatedBy = "test", Created = Today, Latitude = lat, Longitude = lng,
                };
                Context.Customers.Add(patient);
                Context.SaveChanges();
                return patient;
            }

            /// <summary>A trip from home to the hospital, as a file brings it: the place name carries the patient's name.</summary>
            public Trip NewTrip(DateTime? date = null, Customer? patient = null) => new()
            {
                Day = "Tuesday", Date = date ?? Today, CustomerId = (patient ?? Patient).Id, Created = Today,
                PickupAddress = "12 Main St, Miami, FL 33101", PickupLatitude = 25.7700, PickupLongitude = -80.1900,
                DropoffAddress = "1611 NW 12th Ave, Miami, FL 33136", DropoffLatitude = 25.7900, DropoffLongitude = -80.2100,
                Dropoff = $"{(patient ?? Patient).FullName} Dropoof - Appointment",
                Type = TripType.Appointment, Status = TripStatus.Assigned,
            };

            /// <summary>What the import does: store the trip and the file's text. Nothing else.</summary>
            public async Task<Trip> ImportAsync(Trip trip, string? pickupComment, string? dropoffComment)
            {
                var isNew = trip.Id == 0;
                if (isNew) Context.Trips.Add(trip);
                await TripFileComments.StashAsync(Context, trip, isNew, pickupComment, dropoffComment);
                await Context.SaveChangesAsync();
                return trip;
            }

            /// <summary>What the worker does afterwards.</summary>
            public Task<int> WorkAsync(params Trip[] trips) =>
                Notes.ProcessTripsAsync(trips.Select(t => t.Id).ToList(), NotesWork.Compose);
        }

        [Fact]
        public async Task The_import_itself_looks_nothing_up_and_creates_no_place()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), "Ring twice", "Bring ID");

            Assert.Equal(0, await f.Context.Places.CountAsync());
            Assert.Equal(0, await f.Context.PlaceNotes.CountAsync());
            Assert.Equal("Ring twice", trip.PickupComment); // the driver has the file's text from the start
        }

        [Fact]
        public async Task A_reimport_keeps_what_the_office_added_to_the_comment()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), "Dogs in the yard", null);
            await f.WorkAsync(trip);

            // The office types an extra line straight into the comment field and saves.
            trip.PickupComment += " | Patient on 3rd floor today";
            await f.Context.SaveChangesAsync();
            await f.WorkAsync(trip);

            // Tomorrow's file brings the same trip again, with its own comment only.
            await f.ImportAsync(trip, "Dogs in the yard", null);
            await f.WorkAsync(trip);

            Assert.Contains("Patient on 3rd floor today", trip.PickupComment);
            Assert.Contains("Dogs in the yard", trip.PickupComment);
        }

        [Fact]
        public async Task The_pickup_at_the_patient_address_becomes_their_home_and_the_hospital_a_facility()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, null);
            await f.WorkAsync(trip);

            var places = await f.Context.Places.OrderBy(p => p.Id).ToListAsync();
            Assert.Equal(2, places.Count);
            Assert.Equal(PlaceKind.Home, places[0].Kind);
            Assert.Equal(f.Patient.Id, places[0].CustomerId);
            Assert.Equal("Jane Doe Home", places[0].Name);
            Assert.Equal(PlaceKind.Facility, places[1].Kind);
        }

        [Fact]
        public async Task A_new_facility_is_named_by_its_address_never_by_the_file_place_name()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, null);
            await f.WorkAsync(trip);

            var facility = await f.Context.Places.SingleAsync(p => p.Kind == PlaceKind.Facility);
            Assert.Equal("1611 NW 12th Ave, Miami, FL 33136", facility.Name);
            Assert.DoesNotContain("Jane", facility.Name);
        }

        [Fact]
        public async Task A_new_facility_next_to_a_catalog_entry_takes_its_name()
        {
            var f = new Fixture();
            f.Context.CatalogIntegrators.Add(new CatalogIntegrator
            {
                Name = "Jackson Memorial Hospital", IsActive = true, Latitude = 25.79005, Longitude = -80.21005,
                MatchKey = "jackson", SearchText = "jackson",
            });
            await f.Context.SaveChangesAsync();

            var trip = await f.ImportAsync(f.NewTrip(), null, null);
            await f.WorkAsync(trip);

            var facility = await f.Context.Places.SingleAsync(p => p.Kind == PlaceKind.Facility);
            Assert.Equal("Jackson Memorial Hospital", facility.Name);
            Assert.NotNull(facility.CatalogIntegratorId);
        }

        [Fact]
        public async Task Two_patients_going_to_the_same_hospital_share_one_place()
        {
            var f = new Fixture();
            var other = f.NewPatient("John Roe", "50 Ocean Dr", 25.70, -80.13);
            var first = await f.ImportAsync(f.NewTrip(), null, null);
            var second = f.NewTrip(patient: other);
            second.PickupAddress = "50 Ocean Dr, Miami, FL"; second.PickupLatitude = 25.70; second.PickupLongitude = -80.13;
            second.DropoffAddress = "1611 Northwest 12th Avenue, Miami FL 33136"; // spelled another way
            await f.ImportAsync(second, null, null);

            await f.WorkAsync(first, second); // in one batch

            Assert.Equal(1, await f.Context.Places.CountAsync(p => p.Kind == PlaceKind.Facility));
            Assert.Equal(2, await f.Context.Places.CountAsync(p => p.Kind == PlaceKind.Home));
        }

        [Fact]
        public async Task A_new_file_line_is_learnt_once_as_a_note_to_review()
        {
            var f = new Fixture();
            var a = await f.ImportAsync(f.NewTrip(), null, "Enter by the ER door. Bring ID");
            var b = await f.ImportAsync(f.NewTrip(Today.AddDays(1)), null, "enter by the ER door; Bring ID. Call on arrival");
            await f.WorkAsync(a, b);

            var notes = await f.Context.PlaceNotes.OrderBy(n => n.Id).ToListAsync();
            Assert.Equal(new[] { "Enter by the ER door.", "Bring ID", "Call on arrival" }, notes.Select(n => n.Text));
            Assert.All(notes, n => Assert.Equal(NoteStatus.ToReview, n.Status));
        }

        [Fact]
        public async Task A_line_the_office_removed_does_not_come_back_with_the_next_file()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, "Bring ID");
            await f.WorkAsync(trip);
            var note = await f.Context.PlaceNotes.SingleAsync();
            await f.Notes.RemovePlaceNoteAsync(trip.Id, note.Id);

            var next = await f.ImportAsync(f.NewTrip(Today.AddDays(1)), null, "Bring ID");
            await f.WorkAsync(next);

            Assert.Equal(1, await f.Context.PlaceNotes.CountAsync());
            Assert.Equal("Bring ID", next.DropoffComment); // still the file's text for that trip
        }

        [Fact]
        public async Task Another_door_of_the_hospital_is_a_new_entrance_of_the_same_place()
        {
            var f = new Fixture();
            var a = await f.ImportAsync(f.NewTrip(), null, null);
            var er = f.NewTrip(Today.AddDays(1));
            er.DropoffLatitude += 0.003; // about 330 m north: the emergency entrance
            await f.ImportAsync(er, null, null);
            await f.WorkAsync(a, er);

            var hospital = await f.Context.Places.Include(p => p.Entrances).SingleAsync(p => p.Kind == PlaceKind.Facility);
            Assert.Equal(2, hospital.Entrances.Count);
        }

        [Fact]
        public async Task A_place_note_queues_the_trips_still_to_come_and_the_worker_composes_them()
        {
            var f = new Fixture();
            var past = await f.ImportAsync(f.NewTrip(Today.AddDays(-3)), null, null);
            var today = await f.ImportAsync(f.NewTrip(), null, null);
            var tomorrow = await f.ImportAsync(f.NewTrip(Today.AddDays(1)), null, null);
            await f.WorkAsync(past, today, tomorrow);

            await f.Notes.AddPlaceNoteAsync(today.Id, TripSide.Dropoff, new NoteUpsertDto { Text = "Park at level 2" });

            Assert.Equal("Park at level 2", today.DropoffComment); // this trip, at once
            Assert.Contains((tomorrow.Id, NotesWork.Compose), f.Queue.Items); // the rest, through the worker
            Assert.DoesNotContain(f.Queue.Items, i => i.TripId == past.Id);

            await f.Notes.ProcessTripsAsync(f.Queue.Items.Select(i => i.TripId).ToList(), NotesWork.Compose);
            Assert.Equal("Park at level 2", tomorrow.DropoffComment);
            Assert.Null(past.DropoffComment);
        }

        [Fact]
        public async Task A_patient_note_reaches_both_ends_of_their_trip()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, null);
            await f.WorkAsync(trip);

            await f.Notes.AddCustomerNoteAsync(trip.Id, new NoteUpsertDto { Text = "Speaks only English" });

            Assert.Equal("Speaks only English", trip.PickupComment);
            Assert.Equal("Speaks only English", trip.DropoffComment);
        }

        [Fact]
        public async Task The_trip_note_survives_a_new_place_note()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, null);
            await f.WorkAsync(trip);
            await f.Notes.SetTripNoteAsync(trip.Id, TripSide.Dropoff, "Appointment moved to 3rd floor");

            await f.Notes.AddPlaceNoteAsync(trip.Id, TripSide.Dropoff, new NoteUpsertDto { Text = "Park at level 2" });

            Assert.Equal("Appointment moved to 3rd floor | Park at level 2", trip.DropoffComment);
        }

        [Fact]
        public async Task Learning_from_a_finished_trip_does_not_rewrite_its_comment()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, "Bring ID");
            trip.Status = TripStatus.Finished;
            trip.DropoffComment = "What the driver had";
            await f.Context.SaveChangesAsync();

            await f.Notes.ProcessTripsAsync(new[] { trip.Id }, NotesWork.Learn);

            Assert.Equal("What the driver had", trip.DropoffComment);
            Assert.Equal("Bring ID", (await f.Context.PlaceNotes.SingleAsync()).Text);
        }

        [Fact]
        public async Task A_tenant_trip_keeps_its_places_in_the_tenant()
        {
            var f = new Fixture();
            f.Context.Providers.Add(new Provider { Id = 40, Name = "Tenant Co", IsTenant = true });
            await f.Context.SaveChangesAsync();
            var trip = f.NewTrip();
            trip.ProviderId = 40;
            await f.ImportAsync(trip, null, null);
            await f.WorkAsync(trip);

            Assert.All(await f.Context.Places.IgnoreQueryFilters().ToListAsync(), p => Assert.Equal(40, p.OwnerProviderId));
        }

        [Fact]
        public async Task A_driver_does_not_reach_a_trip_of_another_route()
        {
            var f = new Fixture(new Office { UserId = 99, RoleId = 2 });
            var trip = await f.ImportAsync(f.NewTrip(), null, null);

            Assert.Null(await f.Notes.GetAsync(trip.Id));
        }

        [Fact]
        public async Task Outsiders_get_only_what_the_file_said()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), "Ring twice", null);
            await f.WorkAsync(trip);
            await f.Notes.AddCustomerNoteAsync(trip.Id, new NoteUpsertDto { Text = "Dialysis patient, weak" });

            var external = await f.Notes.GetExternalCommentsAsync(new[] { trip });

            Assert.Equal("Ring twice", external[trip.Id].Pickup);
            Assert.Contains("Dialysis patient, weak", trip.PickupComment);
        }

        [Fact]
        public async Task A_trip_from_before_RE054_keeps_its_comment_for_outsiders_and_as_its_own_note()
        {
            var f = new Fixture();
            var old = f.NewTrip();
            old.PickupComment = "Old comment typed by the office";
            f.Context.Trips.Add(old);
            await f.Context.SaveChangesAsync();

            var external = await f.Notes.GetExternalCommentsAsync(new[] { old });
            Assert.Equal("Old comment typed by the office", external[old.Id].Pickup);

            await f.WorkAsync(old);
            Assert.Equal("Old comment typed by the office", old.PickupComment);
        }
    }
}
