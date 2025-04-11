namespace Meditrans.Shared.Entities
{
    public class VehicleRoute
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int DriverId { get; set; }
        public User Driver { get; set; }
        public int VehicleId { get; set; }
        public Vehicle Vehicle { get; set; }
        public string Garage { get; set; }
        public ICollection<Schedule> Schedules { get; set; }
    }
}
