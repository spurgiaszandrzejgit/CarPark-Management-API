
using CarPark.Api.DTOs;
using CarPark.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Tests.Services;

public class ParkingServiceTests
{
    [Fact]
    public async Task ParkVehicleAsync_ShouldAssignFirstAvailableSpace()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 3);

        var request = new ParkVehicleRequest
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        };

        var result = await test.Service.ParkVehicleAsync(request);

        Assert.Equal("WA12345", result.VehicleReg);
        Assert.Equal(1, result.SpaceNumber);

        var session = await test.DbContext.ParkingSessions.SingleAsync();

        Assert.Equal(1, session.ParkingSpaceId);
        Assert.Null(session.TimeOut);
    }

    [Fact]
    public async Task ParkVehicleAsync_ShouldThrow_WhenVehicleAlreadyParked()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 2);

        var request = new ParkVehicleRequest
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        };

        await test.Service.ParkVehicleAsync(request);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Service.ParkVehicleAsync(request));

        Assert.Equal("Vehicle is already parked.", exception.Message);

        Assert.Equal(
            1,
            await test.DbContext.ParkingSessions.CountAsync());
    }

    [Fact]
    public async Task ParkVehicleAsync_ShouldThrow_WhenParkingIsFull()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 1);

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA11111",
            VehicleType = 1
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Service.ParkVehicleAsync(new ParkVehicleRequest
            {
                VehicleReg = "WA22222",
                VehicleType = 2
            }));

        Assert.Equal("No available parking spaces.", exception.Message);

        Assert.Equal(
            1,
            await test.DbContext.ParkingSessions.CountAsync());
    }

    [Fact]
    public async Task ParkVehicleAsync_ShouldReuseSpace_AfterVehicleExit()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 1);

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA11111",
            VehicleType = 1
        });

        test.Clock.Advance(TimeSpan.FromMinutes(5));

        await test.Service.ExitVehicleAsync(new ExitVehicleRequest
        {
            VehicleReg = "WA11111"
        });

        var result = await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA22222",
            VehicleType = 2
        });

        Assert.Equal(1, result.SpaceNumber);

        Assert.Equal(
            2,
            await test.DbContext.ParkingSessions.CountAsync());

        Assert.Equal(
            1,
            await test.DbContext.ParkingSessions
                .CountAsync(s => s.TimeOut == null));
    }

    [Fact]
    public async Task ParkVehicleAsync_ShouldAssignLowestAvailableSpaceNumber()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 4);

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA11111",
            VehicleType = 1
        });

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA22222",
            VehicleType = 2
        });

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA33333",
            VehicleType = 3
        });

        // Free space 2
        await test.Service.ExitVehicleAsync(new ExitVehicleRequest
        {
            VehicleReg = "WA22222"
        });

        var result = await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA44444",
            VehicleType = 1
        });

        Assert.Equal(2, result.SpaceNumber);

        var occupiedSpaceNumbers = await (
            from session in test.DbContext.ParkingSessions
            join space in test.DbContext.ParkingSpaces
                on session.ParkingSpaceId equals space.Id
            where session.TimeOut == null
            orderby space.SpaceNumber
            select space.SpaceNumber
        ).ToArrayAsync();

        Assert.Equal(new[] { 1, 2, 3 }, occupiedSpaceNumbers);
    }

    [Fact]
    public async Task ExitVehicleAsync_ShouldCompleteSessionAndSaveCharge()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 1);

        var parking = await test.Service.ParkVehicleAsync(
            new ParkVehicleRequest
            {
                VehicleReg = "WA12345",
                VehicleType = 1
            });

        // Exactly 10 minutes pass
        test.Clock.Advance(TimeSpan.FromMinutes(10));

        var result = await test.Service.ExitVehicleAsync(
            new ExitVehicleRequest
            {
                VehicleReg = "WA12345"
            });

        Assert.Equal("WA12345", result.VehicleReg);
        Assert.Equal(3.00, result.VehicleCharge);
        Assert.Equal(parking.TimeIn, result.TimeIn);
        Assert.Equal(parking.TimeIn.AddMinutes(10), result.TimeOut);

        // Reload from the database
        test.DbContext.ChangeTracker.Clear();

        var savedSession = await test.DbContext.ParkingSessions.SingleAsync();

        Assert.NotNull(savedSession.TimeOut);
        Assert.Equal(3.00m, savedSession.Charge);
        Assert.Equal(result.TimeOut, savedSession.TimeOut);

        var status = await test.Service.GetParkingStatusAsync();

        Assert.Equal(1, status.AvailableSpaces);
        Assert.Equal(0, status.OccupiedSpaces);
    }

    [Fact]
    public async Task ExitVehicleAsync_ShouldThrow_WhenVehicleAlreadyExited()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 1);

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        });

        test.Clock.Advance(TimeSpan.FromMinutes(5));

        await test.Service.ExitVehicleAsync(new ExitVehicleRequest
        {
            VehicleReg = "WA12345"
        });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Service.ExitVehicleAsync(new ExitVehicleRequest
            {
                VehicleReg = "WA12345"
            }));

        Assert.Equal("Vehicle is not currently parked.", exception.Message);

        Assert.Equal(1, await test.DbContext.ParkingSessions.CountAsync());
    }

    [Fact]
    public async Task ExitVehicleAsync_ShouldThrow_WhenVehicleNotFound()
    {
        await using var test = await ParkingTestContext.CreateAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => test.Service.ExitVehicleAsync(new ExitVehicleRequest
            {
                VehicleReg = "UNKNOWN"
            }));

        Assert.Equal("Vehicle not found.", exception.Message);
    }

    [Fact]
    public async Task GetParkingSpaceDetailsAsync_ShouldCalculateCurrentCharge()
    {
        await using var test = await ParkingTestContext.CreateAsync(capacity: 1);

        await test.Service.ParkVehicleAsync(new ParkVehicleRequest
        {
            VehicleReg = "WA12345",
            VehicleType = 1
        });

        test.Clock.Advance(TimeSpan.FromMinutes(10));

        var details = await test.Service.GetParkingSpaceDetailsAsync(1);

        Assert.NotNull(details);
        Assert.True(details.IsOccupied);
        Assert.Equal("WA12345", details.VehicleReg);
        Assert.Equal(3.00m, details.CurrentCharge);
    }
}
