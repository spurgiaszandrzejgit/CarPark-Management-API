
using CarPark.Api.Models;
using CarPark.Api.Services;

namespace CarPark.Tests.Services;

public class ParkingChargeCalculatorTests
{
    [Theory]
    [InlineData(VehicleType.Small, 0, "0.00")]
    [InlineData(VehicleType.Small, 1, "0.10")]
    [InlineData(VehicleType.Medium, 1, "0.20")]
    [InlineData(VehicleType.Large, 1, "0.40")]
    [InlineData(VehicleType.Small, 4, "0.40")]
    [InlineData(VehicleType.Medium, 5, "2.00")]
    [InlineData(VehicleType.Large, 5, "3.00")]
    [InlineData(VehicleType.Small, 5, "1.50")]
    [InlineData(VehicleType.Small, 6, "1.60")]
    [InlineData(VehicleType.Small, 10, "3.00")]

    public void Calculate_ShouldReturnCorrectCharge(VehicleType vehicleType, int minutes, string expectedCharge)
    {
        var duration = TimeSpan.FromMinutes(minutes);
        var expected = decimal.Parse(expectedCharge, System.Globalization.CultureInfo.InvariantCulture);

        var result = ParkingChargeCalculator.Calculate(vehicleType, duration);

        Assert.Equal(expected, result);
    }


    [Fact]
    public void Calculate_ShouldThrow_WhenDurationIsNegative()
    {
        var duration = TimeSpan.FromMinutes(-1);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ParkingChargeCalculator.Calculate(
                VehicleType.Small, duration));
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenVehicleTypeIsInvalid()
    {
        var invalidType = (VehicleType)99;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ParkingChargeCalculator.Calculate(
                invalidType, TimeSpan.FromMinutes(5)));
    }

}
