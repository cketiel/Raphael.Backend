namespace Meditrans.Shared.Entities
{
    public class Vehicle
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string VIN { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public int Year { get; set; }
        public string Plate { get; set; }
        public bool IsInactive { get; set; }
        public int GroupId { get; set; }
        public VehicleGroup VehicleGroup { get; set; }
        public int CapacityTypeId { get; set; }
        public CapacityType CapacityType { get; set; }
        public ICollection<VehicleRoute> VehicleRoutes { get; set; }
    }
}
