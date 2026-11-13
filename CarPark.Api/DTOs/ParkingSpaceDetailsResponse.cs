namespace CarPark.Api.DTOs
{
    public sealed class ParkingSpaceDetailsResponse
    {
        public int SpaceNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string? VehicleReg { get; set; }
        public int? VehicleType { get; set; }
        public DateTime? TimeIn { get; set; }
        public decimal? CurrentCharge { get; set; }
    }
}
