using Raphael.Api.Services.Places;

namespace Raphael.Api.Tests
{
    /// <summary>Text rules of the place and patient notes (RE-054): keys, lines, repeats.</summary>
    public class NoteTextTests
    {
        [Theory]
        [InlineData("1611 NW 12th Avenue, Miami, FL 33136", "1611 NW 12th Ave, Miami FL 33136")]
        [InlineData("200 South Street Suite 4", "200 S St. #4")]
        [InlineData("12 Main St, Miami, FL, USA", "12 main street miami fl")]
        public void Spellings_of_one_address_share_a_key(string a, string b) =>
            Assert.Equal(NoteText.AddressKey(a), NoteText.AddressKey(b));

        [Fact]
        public void Two_suites_of_one_building_are_two_places() =>
            Assert.NotEqual(NoteText.AddressKey("200 S St Suite 4"), NoteText.AddressKey("200 S St Suite 5"));

        [Fact]
        public void A_trip_address_with_city_is_the_street_address_on_the_patient_record() =>
            Assert.True(NoteText.SameStreetAddress(
                NoteText.AddressKey("12 Main Street, Miami, FL 33101"), NoteText.AddressKey("12 Main St")));

        [Fact]
        public void A_different_number_is_not_the_patient_home() =>
            Assert.False(NoteText.SameStreetAddress(
                NoteText.AddressKey("120 Main Street, Miami"), NoteText.AddressKey("12 Main St")));

        [Fact]
        public void A_comment_splits_by_sentence_but_not_after_an_abbreviation()
        {
            var lines = NoteText.SplitLines("Ring Apt. 4 twice. Dogs in the yard; Patient speaks only English");
            Assert.Equal(new[] { "Ring Apt. 4 twice.", "Dogs in the yard", "Patient speaks only English" }, lines);
        }

        [Fact]
        public void Case_and_punctuation_do_not_make_a_line_new() =>
            Assert.Empty(NoteText.NewLines("DOGS in the yard!!", new[] { "Dogs in the yard." }));

        [Fact]
        public void A_line_contained_in_a_known_note_is_not_new() =>
            Assert.Empty(NoteText.NewLines("gate code 1234", new[] { "Gate code 1234, ring twice" }));

        [Fact]
        public void Only_the_new_lines_of_a_comment_are_new()
        {
            var lines = NoteText.NewLines("Gate code 1234. Wheelchair ramp at the back.", new[] { "gate code 1234" });
            Assert.Equal(new[] { "Wheelchair ramp at the back." }, lines);
        }

        [Fact]
        public void A_line_repeated_inside_one_comment_is_learnt_once() =>
            Assert.Single(NoteText.NewLines("Call on arrival. Call on arrival.", Array.Empty<string>()));
    }

    /// <summary>The comment the driver reads, and the office's edits read back (RE-054).</summary>
    public class NoteComposerTests
    {
        private static StopNoteSources Sources(string? tripNote = null, string[]? entrance = null, string[]? place = null,
            string[]? customer = null, string? file = null) =>
            new(tripNote, entrance ?? Array.Empty<string>(), place ?? Array.Empty<string>(), customer ?? Array.Empty<string>(), file);

        [Fact]
        public void The_trip_note_comes_first_then_entrance_place_patient_and_file()
        {
            var composed = NoteComposer.Compose(Sources("Today 3rd floor", new[] { "Use ER door" }, new[] { "Dogs" },
                new[] { "Speaks only English" }, "Bring ID"));
            Assert.Equal("Today 3rd floor | Use ER door | Dogs | Speaks only English | Bring ID", composed);
        }

        [Fact]
        public void The_file_line_a_note_already_says_is_not_said_twice() =>
            Assert.Equal("Dogs in the yard", NoteComposer.Compose(Sources(place: new[] { "Dogs in the yard" }, file: "dogs in the yard.")));

        [Fact]
        public void Nothing_to_say_is_null() =>
            Assert.Null(NoteComposer.Compose(Sources()));

        [Fact]
        public void An_unchanged_comment_leaves_no_trip_note()
        {
            var current = Sources(place: new[] { "Dogs" }, customer: new[] { "Speaks only English" }, file: "Bring ID");
            Assert.Null(NoteComposer.TripNoteFromEditedComment("Dogs | Speaks only English | Bring ID", current));
        }

        [Fact]
        public void What_the_office_typed_beyond_the_notes_is_the_trip_note()
        {
            var current = Sources(place: new[] { "Dogs" }, file: "Bring ID");
            Assert.Equal("Patient on 3rd floor today",
                NoteComposer.TripNoteFromEditedComment("Dogs | Bring ID | Patient on 3rd floor today", current));
        }

        [Fact]
        public void A_long_composition_drops_its_last_lines_and_keeps_the_trip_note()
        {
            var many = Enumerable.Range(1, 400).Select(i => $"Place note number {i}").ToArray();
            var composed = NoteComposer.Compose(Sources("Trip note", place: many))!;
            Assert.StartsWith("Trip note | ", composed);
            Assert.True(composed.Length <= Raphael.Shared.Entities.Places.NoteRules.MaxComposedLength);
        }
    }
}
