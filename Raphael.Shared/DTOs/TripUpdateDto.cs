namespace Raphael.Shared.DTOs
{
    public class TripUpdateDto : TripCreateDto
    {
        /// <summary>
        /// True only when the client means to change the Provider (RE-050). Older Desktops never
        /// send it, so they never touch the Provider. Only Raphael's admin may set it.
        /// </summary>
        public bool SetProvider { get; set; }

        /// <summary>The Provider to give the trip to; null gives it back to Raphael. Read only with <see cref="SetProvider"/>.</summary>
        public int? ProviderId { get; set; }

        public bool IsCancelled { get; set; }
    }

}

