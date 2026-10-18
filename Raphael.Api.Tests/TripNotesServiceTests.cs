using Microsoft.EntityFrameworkCore;
using Raphael.Api.Services.Places;
using Raphael.Api.Services.Trips;
using Raphael.Shared.DbContexts;
using Raphael.Shared.DTOs.Places;
using Raphael.Shared.Entities;
using Raphael.Shared.Entities.Places;
using Raphael.Shared.Interfaces;
using Raphael.Shared.Time;

namespace Raphael.Api.Tests
{
    /// <summary>
    /// Places and notes against a database (EF InMemory): finding places, learning from files, and
    /// the re-import that used to erase the office's notes (RE-054).
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

        private sealed class Fixture
        {
            public RaphaelContext Context { get; }
            public TripNotesService Notes { get; }
            public Customer Patient { get; }

            public Fixture(ICurrentUserService? user = null)
            {
                user ??= new Office();
                var options = new DbContextOptionsBuilder<RaphaelContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
                Context = new RaphaelContext(options, user);
                Notes = new TripNotesService(Context, user, new Clock(), new OpenGuard());

                Patient = new Customer
                {
                    FullName = "Jane Doe", Address = "12 Main Street", City = "Miami", State = "FL", Zip = "33101",
                    Gender = "F", CreatedBy = "test", Created = Today, Latitude = 25.7700, Longitude = -80.1900,
                };
                Context.Customers.Add(Patient);
                Context.SaveChanges();
            }

            /// <summary>A trip from home to the hospital, as a file would bring it.</summary>
            public Trip NewTrip(DateTime? date = null) => new()
            {
                Day = "Tuesday", Date = date ?? Today, CustomerId = Patient.Id, Created = Today,
                PickupAddress = "12 Main St, Miami, FL 33101", PickupLatitude = 25.7700, PickupLongitude = -80.1900,
                DropoffAddress = "1611 NW 12th Ave, Miami, FL 33136", DropoffLatitude = 25.7900, DropoffLongitude = -80.2100,
                Dropoff = "Jackson Memorial Hospital", Type = TripType.Appointment, Status = TripStatus.Assigned,
            };

            public async Task<Trip> ImportAsync(Trip trip, string? pickupComment, string? dropoffComment)
            {
                if (trip.Id == 0) Context.Trips.Add(trip);
                await Notes.ApplyIncomingAsync(trip, pickupComment, dropoffComment, CommentOrigin.External);
                await Context.SaveChangesAsync();
                return trip;
            }
        }

        [Fact]
        public async Task A_reimport_keeps_what_the_office_added_to_the_comment()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), "Dogs in the yard", null);

            // The office types an extra line straight into the comment field (Desktop up to 1.13).
            await f.Notes.ApplyIncomingAsync(trip, trip.PickupComment + " | Patient on 3rd floor today", null, CommentOrigin.Office);
            await f.Context.SaveChangesAsync();

            // Tomorrow's file brings the same trip again, with its own comment only.
            await f.ImportAsync(trip, "Dogs in the yard", null);

            Assert.Contains("Patient on 3rd floor today", trip.PickupComment);
            Assert.Contains("Dogs in the yard", trip.PickupComment);
        }

        [Fact]
        public async Task The_pickup_at_the_patient_address_becomes_their_home_and_the_hospital_a_facility()
        {
            var f = new Fixture();
            await f.ImportAsync(f.NewTrip(), null, null);

            var places = await f.Context.Places.OrderBy(p => p.Id).ToListAsync();
            Assert.Equal(2, places.Count);
            Assert.Equal(PlaceKind.Home, places[0].Kind);
            Assert.Equal(f.Patient.Id, places[0].CustomerId);
            Assert.Equal("Jane Doe Home", places[0].Name);
            Assert.Equal(PlaceKind.Facility, places[1].Kind);
            Assert.Equal("Jackson Memorial Hospital", places[1].Name);
        }

        [Fact]
        public async Task A_new_file_line_is_learnt_once_as_a_note_to_review()
        {
            var f = new Fixture();
            await f.ImportAsync(f.NewTrip(), null, "Enter by the ER door. Bring ID");
            await f.ImportAsync(f.NewTrip(Today.AddDays(1)), null, "enter by the ER door; Bring ID. Call on arrival");

            var notes = await f.Context.PlaceNotes.OrderBy(n => n.Id).ToListAsync();
            Assert.Equal(new[] { "Enter by the ER door.", "Bring ID", "Call on arrival" }, notes.Select(n => n.Text));
            Assert.All(notes, n => Assert.Equal(NoteStatus.ToReview, n.Status));
            Assert.All(notes, n => Assert.Equal(NoteSource.Import, n.Source));
        }

        [Fact]
        public async Task A_line_the_office_removed_does_not_come_back_with_the_next_file()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, "Bring ID");
            var note = await f.Context.PlaceNotes.SingleAsync();
            await f.Notes.RemovePlaceNoteAsync(trip.Id, note.Id);

            var next = await f.ImportAsync(f.NewTrip(Today.AddDays(1)), null, "Bring ID");

            Assert.Equal(1, await f.Context.PlaceNotes.CountAsync());
            // What the file said still reaches the driver of that trip: it is the file's text.
            Assert.Equal("Bring ID", next.DropoffComment);
        }

        [Fact]
        public async Task The_same_hospital_spelled_another_way_is_the_same_place()
        {
            var f = new Fixture();
            await f.ImportAsync(f.NewTrip(), null, null);

            var other = f.NewTrip(Today.AddDays(1));
            other.DropoffAddress = "1611 Northwest 12th Avenue, Miami FL 33136";
            await f.ImportAsync(other, null, null);

            Assert.Equal(1, await f.Context.Places.CountAsync(p => p.Kind == PlaceKind.Facility));
        }

        [Fact]
        public async Task Another_door_of_the_hospital_is_a_new_entrance_of_the_same_place()
        {
            var f = new Fixture();
            await f.ImportAsync(f.NewTrip(), null, null);

            var er = f.NewTrip(Today.AddDays(1));
            er.DropoffLatitude += 0.003; // about 330 m north: the emergency entrance
            await f.ImportAsync(er, null, null);

            var hospital = await f.Context.Places.Include(p => p.Entrances).SingleAsync(p => p.Kind == PlaceKind.Facility);
            Assert.Equal(2, hospital.Entrances.Count);
        }

        [Fact]
        public async Task A_place_note_reaches_the_trips_still_to_come_and_not_the_past_ones()
        {
            var f = new Fixture();
            var past = await f.ImportAsync(f.NewTrip(Today.AddDays(-3)), null, null);
            var today = await f.ImportAsync(f.NewTrip(), null, null);
            var tomorrow = await f.ImportAsync(f.NewTrip(Today.AddDays(1)), null, null);

            await f.Notes.AddPlaceNoteAsync(today.Id, TripSide.Dropoff, new NoteUpsertDto { Text = "Park at level 2" });

            Assert.Equal("Park at level 2", today.DropoffComment);
            Assert.Equal("Park at level 2", tomorrow.DropoffComment);
            Assert.Null(past.DropoffComment);
        }

        [Fact]
        public async Task A_patient_note_reaches_both_ends_of_their_trips()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, null);

            await f.Notes.AddCustomerNoteAsync(trip.Id, new NoteUpsertDto { Text = "Speaks only English" });

            Assert.Equal("Speaks only English", trip.PickupComment);
            Assert.Equal("Speaks only English", trip.DropoffComment);
        }

        [Fact]
        public async Task The_trip_note_survives_a_refresh_from_the_place()
        {
            var f = new Fixture();
            var trip = await f.ImportAsync(f.NewTrip(), null, null);
            await f.Notes.SetTripNoteAsync(trip.Id, TripSide.Dropoff, "Appointment moved to 3rd floor");

            await f.Notes.AddPlaceNoteAsync(trip.Id, TripSide.Dropoff, new NoteUpsertDto { Text = "Park at level 2" });

            Assert.Equal("Appointment moved to 3rd floor | Park at level 2", trip.DropoffComment);
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
            await f.Notes.AddCustomerNoteAsync(trip.Id, new NoteUpsertDto { Text = "Dialysis patient, weak" });

            var external = await f.Notes.GetExternalCommentsAsync(new[] { trip });

            Assert.Equal("Ring twice", external[trip.Id].Pickup);
            Assert.Contains("Dialysis patient, weak", trip.PickupComment);
        }

        [Fact]
        public async Task The_history_seed_writes_nothing_in_a_dry_run_and_runs_twice_without_repeating()
        {
            var f = new Fixture();
            foreach (var day in new[] { -10, -9 })
            {
                var old = f.NewTrip(Today.AddDays(day));
                old.DropoffComment = "Enter by the ER door";
                f.Context.Trips.Add(old);
            }
            await f.Context.SaveChangesAsync();

            var dry = await f.Notes.SeedFromHistoryAsync(Today.AddDays(-30), Today, dryRun: true);
            Assert.Equal(1, dry.FacilitiesCreated);
            Assert.Equal(1, dry.NotesCreated);
            Assert.Equal(0, await f.Context.Places.CountAsync());

            await f.Notes.SeedFromHistoryAsync(Today.AddDays(-30), Today, dryRun: false);
            var again = await f.Notes.SeedFromHistoryAsync(Today.AddDays(-30), Today, dryRun: false);

            Assert.Equal(2, await f.Context.Places.CountAsync());
            Assert.Equal(1, await f.Context.PlaceNotes.CountAsync(n => n.Source == NoteSource.History && n.Status == NoteStatus.ToReview));
            Assert.Equal(0, again.NotesCreated + again.FacilitiesCreated + again.HomesCreated);
            // Past trips are history: the seed does not rewrite them.
            Assert.Equal(0, await f.Context.TripStopNotes.CountAsync());
        }

        [Fact]
        public async Task A_trip_from_before_RE054_keeps_its_comment_for_outsiders()
        {
            var f = new Fixture();
            var old = f.NewTrip();
            old.PickupComment = "Old comment";
            f.Context.Trips.Add(old);
            await f.Context.SaveChangesAsync();

            var external = await f.Notes.GetExternalCommentsAsync(new[] { old });

            Assert.Equal("Old comment", external[old.Id].Pickup);
        }
    }
}
