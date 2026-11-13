namespace CarPark.Api.DTOs
{
    public class ParkVehicleRequest
    {
        public required string VehicleReg { get; set; }
        public int VehicleType { get; set; }
    }
}
