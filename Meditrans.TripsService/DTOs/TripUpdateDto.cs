namespace Raphael.TripsService.DTOs
{
    public class TripUpdateDto : TripCreateDto
    {
        public bool IsCancelled { get; set; }
    }

}

