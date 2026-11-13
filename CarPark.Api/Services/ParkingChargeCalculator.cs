
using CarPark.Api.Models;

namespace CarPark.Api.Services;

public static class ParkingChargeCalculator
{
    public static decimal Calculate(VehicleType vehicleType, TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration));

        decimal ratePerMinute = vehicleType switch
        {
            VehicleType.Small => 0.10m,
            VehicleType.Medium => 0.20m,
            VehicleType.Large => 0.40m,
            _ => throw new ArgumentOutOfRangeException(nameof(vehicleType))
        };

        var minutes = (int)Math.Ceiling(duration.TotalMinutes);
        var fiveMinuteBlocks = minutes / 5;

        return minutes * ratePerMinute + fiveMinuteBlocks * 1.00m;
    }
}
