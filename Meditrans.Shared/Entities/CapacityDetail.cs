namespace Meditrans.Shared.Entities
{
    public class CapacityDetail
    {
        public int Id { get; set; }
        public int SpaceTypeId { get; set; }
        public SpaceType SpaceType { get; set; }
        public int Quantity { get; set; }
    }
}
