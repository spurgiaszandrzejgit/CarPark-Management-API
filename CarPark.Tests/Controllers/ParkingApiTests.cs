
using CarPark.Api.DTOs;
using CarPark.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;

namespace CarPark.Tests.Controllers;

public class ParkingApiTests
{
    [Fact]
    public async Task PostParking_ShouldReturnOk()
    {

        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        };

        var response = await client.PostAsJsonAsync(
            "/parking",
            request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ParkVehicleResponse>();

        Assert.NotNull(result);
        Assert.Equal("WA12345", result.VehicleReg);
        Assert.Equal(1, result.SpaceNumber);
    }


    [Fact]
    public async Task GetParking_ShouldReturnCorrectStatus()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        await client.PostAsJsonAsync("/parking", new
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        });

        var response = await client.GetAsync("/parking");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<ParkingStatusResponse>();

        Assert.NotNull(result);
        Assert.Equal(2, result.AvailableSpaces);
        Assert.Equal(1, result.OccupiedSpaces);
    }


    [Fact]
    public async Task PostParking_ShouldReturnConflict_WhenVehicleAlreadyParked()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        };

        var firstResponse = await client.PostAsJsonAsync(
            "/parking", request);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        var response = await client.PostAsJsonAsync(
            "/parking", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }


    [Fact]
    public async Task PostParkingExit_ShouldReturnOkAndFreeSpace()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var parkingResponse = await client.PostAsJsonAsync(
            "/parking",
            new
            {
                VehicleReg = "WA12345",
                VehicleType = 1
            });

        Assert.Equal(HttpStatusCode.OK, parkingResponse.StatusCode);

        var exitResponse = await client.PostAsJsonAsync(
            "/parking/exit",
            new { VehicleReg = "WA12345" });

        Assert.Equal(HttpStatusCode.OK, exitResponse.StatusCode);

        var exitResult = await exitResponse.Content
            .ReadFromJsonAsync<ExitVehicleResponse>();

        Assert.NotNull(exitResult);
        Assert.Equal("WA12345", exitResult.VehicleReg);
        Assert.True(exitResult.TimeOut >= exitResult.TimeIn);

        var statusResponse = await client.GetAsync("/parking");
        statusResponse.EnsureSuccessStatusCode();

        var status = await statusResponse.Content
            .ReadFromJsonAsync<ParkingStatusResponse>();

        Assert.NotNull(status);
        Assert.Equal(3, status.AvailableSpaces);
        Assert.Equal(0, status.OccupiedSpaces);
    }


    [Fact]
    public async Task PostParking_ShouldReturnBadRequest_WhenVehicleTypeIsInvalid()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/parking", new
        {
            VehicleReg = "WA12345",
            VehicleType = 99
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostParking_ShouldReturnConflict_WhenParkingIsFull()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        for (int i = 1; i <= 3; i++)
        {
            var parkingResponse = await client.PostAsJsonAsync("/parking", new
            {
                VehicleReg = $"WA{i}",
                VehicleType = 1
            });

            Assert.Equal(HttpStatusCode.OK, parkingResponse.StatusCode);
        }

        var response = await client.PostAsJsonAsync("/parking", new
        {
            VehicleReg = "WA4",
            VehicleType = 1
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostParkingExit_ShouldReturnConflict_WhenVehicleNotFound()
    {
        using var factory = new CarParkWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/parking/exit", new
        {
            VehicleReg = "UNKNOWN"
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

}
