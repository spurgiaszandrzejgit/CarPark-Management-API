
using CarPark.Api.DTOs;
using CarPark.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace CarPark.Tests;

public class ParkingSpacesEndpointTests
{
    [Fact]
    public async Task GetSpaces_WhenParkingIsEmpty_ReturnsAllAvailable()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/parking/spaces");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var spaces = await response.Content
            .ReadFromJsonAsync<List<ParkingSpaceDto>>();

        Assert.NotNull(spaces);
        Assert.Equal(3, spaces.Count);
        Assert.All(spaces, space =>
        {
            Assert.False(space.IsOccupied);
            Assert.Null(space.VehicleReg);
        });
    }

    [Fact]
    public async Task GetSpaces_AfterVehicleEntry_ReturnsOccupiedSpace()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var entryResponse = await client.PostAsJsonAsync(
            "/parking",
            new { vehicleReg = "WA12345", vehicleType = 1 });

        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        var spaces = await client.GetFromJsonAsync<List<ParkingSpaceDto>>(
            "/parking/spaces");

        Assert.NotNull(spaces);
        Assert.Equal(1, spaces.Count(s => s.IsOccupied));

        var occupied = Assert.Single(spaces, s => s.IsOccupied);

        Assert.Equal(1, occupied.SpaceNumber);
        Assert.Equal("WA12345", occupied.VehicleReg);
    }

    [Fact]
    public async Task GetSpaces_AfterVehicleExit_ReturnsAvailableSpace()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var entryResponse = await client.PostAsJsonAsync(
            "/parking",
            new { vehicleReg = "WA12345", vehicleType = 1 });

        Assert.Equal(HttpStatusCode.OK, entryResponse.StatusCode);

        var exitResponse = await client.PostAsJsonAsync(
            "/parking/exit",
            new { vehicleReg = "WA12345" });

        Assert.Equal(HttpStatusCode.OK, exitResponse.StatusCode);

        var spaces = await client.GetFromJsonAsync<List<ParkingSpaceDto>>(
            "/parking/spaces");

        Assert.NotNull(spaces);
        Assert.Equal(3, spaces.Count);
        Assert.All(spaces, space =>
        {
            Assert.False(space.IsOccupied);
            Assert.Null(space.VehicleReg);
        });
    }

    [Fact]
    public async Task GetSpaceDetails_WhenSpaceDoesNotExist_ReturnsNotFound()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/parking/spaces/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetSpaceDetails_WhenSpaceIsAvailable_ReturnsEmptySession()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/parking/spaces/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content
            .ReadFromJsonAsync<ParkingSpaceDetailsResponse>();

        Assert.NotNull(details);
        Assert.Equal(1, details.SpaceNumber);
        Assert.False(details.IsOccupied);
        Assert.Null(details.VehicleReg);
        Assert.Null(details.VehicleType);
        Assert.Null(details.TimeIn);
        Assert.Null(details.CurrentCharge);
    }

    [Fact]
    public async Task GetSpaceDetails_WhenOccupied_ReturnsActiveSession()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var parkResponse = await client.PostAsJsonAsync(
            "/parking",
            new { vehicleReg = "WA12345", vehicleType = 1 });

        Assert.Equal(HttpStatusCode.OK, parkResponse.StatusCode);

        var parking = await parkResponse.Content
            .ReadFromJsonAsync<ParkVehicleResponse>();

        Assert.NotNull(parking);

        var response = await client.GetAsync(
            $"/parking/spaces/{parking.SpaceNumber}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var details = await response.Content
            .ReadFromJsonAsync<ParkingSpaceDetailsResponse>();

        Assert.NotNull(details);
        Assert.True(details.IsOccupied);
        Assert.Equal(parking.SpaceNumber, details.SpaceNumber);
        Assert.Equal("WA12345", details.VehicleReg);
        Assert.Equal(1, details.VehicleType);
        Assert.Equal(parking.TimeIn, details.TimeIn);
        Assert.NotNull(details.CurrentCharge);
        Assert.True(details.CurrentCharge >= 0);
    }

    private sealed class ParkingSpaceDto
    {
        public int SpaceNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string? VehicleReg { get; set; }
    }
}
