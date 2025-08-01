namespace Raphael.TripsService.Dtos
{
    public class SpaceTypeDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public float LoadTime { get; set; }
        public float UnloadTime { get; set; }
        public int CapacityTypeId { get; set; }
        public bool IsActive { get; set; }
    }
}

