using System.ComponentModel.DataAnnotations;

namespace Raphael.Shared.DTOs
{
    public class RideCenterTripDto
    {
        [Required]
        public string TripId { get; set; } 
        [Required]
        public DateTime Date { get; set; }
        public TimeSpan? FromTime { get; set; }
        public TimeSpan? ToTime { get; set; }

        // Datos del Cliente
        public string? RiderId { get; set; }
        [Required]
        public string CustomerFullName { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public string? CustomerCity { get; set; }
        public string? CustomerZip { get; set; }
        public string? CustomerGender { get; set; } = "Unknown";

        // Referencias por Nombre
        [Required]
        public string SpaceTypeName { get; set; }
        [Required]
        public string FundingSourceName { get; set; }

        // Datos del Viaje
        [Required]
        public string PickupAddress { get; set; }
        public double PickupLatitude { get; set; }
        public double PickupLongitude { get; set; }
        [Required]
        public string DropoffAddress { get; set; }
        public double DropoffLatitude { get; set; }
        public double DropoffLongitude { get; set; }
        public string? Type { get; set; } // Appointment / Return
        public string? Authorization { get; set; }
    }
}
