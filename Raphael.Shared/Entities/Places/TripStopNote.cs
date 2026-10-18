using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Places
{
    /// <summary>The end of a trip a <see cref="TripStopNote"/> is about.</summary>
    public enum TripSide
    {
        Pickup = 1,
        Dropoff = 2,
    }

    /// <summary>
    /// What one end of a trip knows about its place and its notes (RE-054). One row per trip and
    /// side, created the first time the trip is saved after RE-054.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A table of its own and not columns on <see cref="Trip"/>: Trips is the largest and busiest
    /// table, and RE-054 adds nothing to it. Trips from before RE-054 have no row and behave as
    /// they always did.
    /// </para>
    /// <para>
    /// <see cref="Trip.PickupComment"/>/<see cref="Trip.DropoffComment"/> keep the text the driver
    /// reads, composed from <see cref="TripNote"/>, the place, the entrance, the patient and
    /// <see cref="FileComment"/>. It is the record of what the trip carried, so it is stored and
    /// not built on every read.
    /// </para>
    /// </remarks>
    public class TripStopNote
    {
        public int Id { get; set; }

        public int TripId { get; set; }
        public Trip Trip { get; set; } = null!;

        public TripSide Side { get; set; }

        public int? PlaceId { get; set; }
        public Place? Place { get; set; }

        public int? PlaceEntranceId { get; set; }
        public PlaceEntrance? PlaceEntrance { get; set; }

        /// <summary>
        /// The comment exactly as the file, the clinic or the integrator sent it. The only text the
        /// Rider app and the integration API get: the rest is the office's notes, PHI included.
        /// A re-import replaces this and nothing else.
        /// </summary>
        public string? FileComment { get; set; }

        /// <summary>What the office or the driver wrote for this trip only.</summary>
        [MaxLength(NoteRules.MaxTripNoteLength)]
        public string? TripNote { get; set; }
    }
}
