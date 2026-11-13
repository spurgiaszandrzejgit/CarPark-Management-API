namespace CarPark.Api.DTOs
{
    public class ExitVehicleResponse
    {
        public required string VehicleReg { get; set; }
        public double VehicleCharge { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime TimeOut { get; set; }
    }
}
