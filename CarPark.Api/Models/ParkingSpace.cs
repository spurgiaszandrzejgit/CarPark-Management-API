namespace CarPark.Api.Models
{
    public class ParkingSpace
    {
        public int Id { get; set; }
        public int SpaceNumber { get; set; }

        public ICollection<ParkingSession> ParkingSessions { get; set; } = new List<ParkingSession>();
    }
}
