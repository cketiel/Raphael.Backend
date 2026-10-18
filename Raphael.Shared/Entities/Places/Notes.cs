using System.ComponentModel.DataAnnotations;
using Raphael.Shared.Interfaces;

namespace Raphael.Shared.Entities.Places
{
    /// <summary>Who wrote a note, or where a row came from (RE-054).</summary>
    public enum NoteSource
    {
        /// <summary>A trip file, a clinic booking or the integration API.</summary>
        Import = 1,
        Dispatcher = 2,
        Driver = 3,
        /// <summary>The one-off seed from the trip history.</summary>
        History = 4,
    }

    /// <summary>Whether a note reaches the driver, and whether the office has looked at it.</summary>
    public enum NoteStatus
    {
        /// <summary>Confirmed. Reaches the driver.</summary>
        Active = 1,

        /// <summary>
        /// New text a file brought, or the history seed. Reaches the driver too, but is flagged for
        /// the office: a file mixes place notes with things that only held for one trip.
        /// </summary>
        ToReview = 2,

        /// <summary>Deleted. Kept, not erased, so who removed what stays on record.</summary>
        Removed = 3,
    }

    /// <summary>Fields every note shares.</summary>
    public abstract class NoteBase
    {
        public int Id { get; set; }

        /// <summary>One line of guidance. PHI on a home or a patient: never logged.</summary>
        [Required]
        [MaxLength(NoteRules.MaxNoteLength)]
        public string Text { get; set; } = string.Empty;

        public NoteSource Source { get; set; }
        public NoteStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }
        public int? CreatedByUserId { get; set; }

        [MaxLength(100)]
        public string? CreatedByName { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
        public int? UpdatedByUserId { get; set; }

        [MaxLength(100)]
        public string? UpdatedByName { get; set; }
    }

    /// <summary>A note about a place, or about one of its entrances (RE-054).</summary>
    public class PlaceNote : NoteBase
    {
        public int PlaceId { get; set; }
        public Place Place { get; set; } = null!;

        /// <summary>Set when the note is about one door of the place and not the whole of it.</summary>
        public int? PlaceEntranceId { get; set; }
        public PlaceEntrance? PlaceEntrance { get; set; }
    }

    /// <summary>
    /// A note about the patient, wherever they go: language, condition, help they need (RE-054).
    /// Plain text: it does not constrain routing.
    /// </summary>
    public class CustomerNote : NoteBase, IOwnedByProvider
    {
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        /// <inheritdoc />
        public int? OwnerProviderId { get; set; }
    }

    /// <summary>Limits shared by the notes and the text composed from them.</summary>
    public static class NoteRules
    {
        public const int MaxNoteLength = 500;

        /// <summary>What a trip may carry for the driver at one stop. Past it the last lines are dropped; the trip's own note goes first and stays.</summary>
        public const int MaxComposedLength = 4000;

        public const int MaxTripNoteLength = 2000;

        /// <summary>Separates the lines of a composed comment. Kept on one line: Desktop's grids show one row per trip.</summary>
        public const string Separator = " | ";
    }
}
