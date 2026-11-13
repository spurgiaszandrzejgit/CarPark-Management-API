namespace CarPark.Api.DTOs
{
    public class ParkingSpaceResponse
    {
        public int SpaceNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string? VehicleReg { get; set; }
    }
}
