using System;

namespace Raphael.Shared.DTOs
{
    public class TripDispatchUpdateDto
    {
        public string Type { get; set; }
        public TimeSpan? FromTime { get; set; }
        public bool WillCall { get; set; }
        public string? PickupPhone { get; set; }
        public string? PickupComment { get; set; }
        public string? DropoffPhone { get; set; }
        public string? DropoffComment { get; set; }
    }
}
