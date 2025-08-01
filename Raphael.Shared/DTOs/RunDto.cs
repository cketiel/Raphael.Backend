namespace Raphael.Shared.DTOs
{
    public class RunDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public int DriverId { get; set; }
        public int VehicleId { get; set; }
        public string? Garage { get; set; }
    }

}

