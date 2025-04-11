using System.Security.AccessControl;

namespace Meditrans.Shared.Entities
{
    public class CapacityType
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<SpaceType> SpaceTypes { get; set; }
        public ICollection<Vehicle> Vehicles { get; set; }
    }
}
