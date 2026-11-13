namespace CarPark.Api.DTOs;

public class ParkVehicleResponse
{
    public required string VehicleReg { get; set; }
    public int SpaceNumber { get; set; }
    public DateTime TimeIn { get; set; }
}