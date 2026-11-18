namespace CarPark.Api.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public required string VehicleReg { get; set; }
        public VehicleType VehicleType { get; set; }

        public ICollection<ParkingSession> ParkingSessions { get; set; } = new List<ParkingSession>();
    }
}
