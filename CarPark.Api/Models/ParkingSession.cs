namespace CarPark.Api.Models
{
    public class ParkingSession
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public int ParkingSpaceId { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public decimal? Charge { get; set; }

        // Navigation properties
        public Vehicle Vehicle { get; set; } = null!;
        public ParkingSpace ParkingSpace { get; set; } = null!;

    }
}
