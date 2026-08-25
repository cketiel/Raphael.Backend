using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.Entities
{
    public class Provider
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(150)]
        public string Name { get; set; }

        public string? Address { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Logo { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        /// <summary>
        /// The timezone this provider's trips are operated in. IANA identifier.
        /// </summary>
        /// <remarks>
        /// This is what a pickup time means. A trip at 09:15 is 09:15 here, whoever opens the
        /// screen and wherever the server happens to be hosted.
        ///
        /// <para>
        /// Nullable because the column arrived after the rows did. A provider that has not
        /// declared one falls back to the configured default, never to the server's own
        /// timezone — see <c>OperationTimeOptions</c>. The Providers screen flags the ones
        /// still empty, so the fallback does not become permanent by inattention.
        /// </para>
        /// </remarks>
        [MaxLength(64)]
        public string? TimeZoneId { get; set; }
    }
}

