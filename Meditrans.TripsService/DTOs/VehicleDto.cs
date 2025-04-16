namespace Meditrans.TripsService.DTOs
{
    public class VehicleDto
    {
        public string Name { get; set; }
        public string? VIN { get; set; }
        public string? Make { get; set; }
        public string? Model { get; set; }
        public string? Color { get; set; }
        public int? Year { get; set; }
        public string? Plate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public bool IsInactive { get; set; }
        public int GroupId { get; set; }
        public int CapacityDetailTypeId { get; set; }
        public int VehicleTypeId { get; set; }
    }


}
