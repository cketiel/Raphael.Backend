using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities.Places
{
    /// <summary>
    /// One point of a <see cref="Place"/> where trips actually stop: the same hospital address can
    /// have the emergency door, outpatients and the dialysis unit a block apart (RE-054).
    /// </summary>
    /// <remarks>
    /// Created from the coordinates trips arrive with; a trip that stops within
    /// <c>PlaceMatcher.SameEntranceMeters</c> of an existing one uses it.
    /// </remarks>
    public class PlaceEntrance
    {
        public int Id { get; set; }

        public int PlaceId { get; set; }
        public Place Place { get; set; } = null!;

        /// <summary>"Emergency", "Main entrance"… Null until somebody names it.</summary>
        [MaxLength(100)]
        public string? Label { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }
    }
}
