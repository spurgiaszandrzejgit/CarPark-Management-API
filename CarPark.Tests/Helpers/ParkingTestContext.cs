
using CarPark.Api.Data;
using CarPark.Api.Models;
using CarPark.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace CarPark.Tests.Helpers;

public sealed class ParkingTestContext : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    public CarParkDbContext DbContext { get; }
    public ParkingService Service { get; }
    public FakeTimeProvider Clock { get; }

    private ParkingTestContext(
        SqliteConnection connection,
        CarParkDbContext dbContext,
        FakeTimeProvider clock)
    {
        _connection = connection;
        DbContext = dbContext;
        Clock = clock;
        Service = new ParkingService(dbContext, clock);
    }

    public static async Task<ParkingTestContext> CreateAsync(int capacity = 3)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        try
        {
            var options = new DbContextOptionsBuilder<CarParkDbContext>()
                .UseSqlite(connection)
                .Options;

            var dbContext = new CarParkDbContext(options);

            try
            {
                await dbContext.Database.EnsureCreatedAsync();

                for (int i = 1; i <= capacity; i++)
                {
                    dbContext.ParkingSpaces.Add(new ParkingSpace
                    {
                        SpaceNumber = i
                    });
                }

                await dbContext.SaveChangesAsync();

                var clock = new FakeTimeProvider(
                    new DateTimeOffset(
                        2026, 11, 13, 10, 0, 0,
                        TimeSpan.Zero));

                return new ParkingTestContext(
                    connection,
                    dbContext,
                    clock);
            }
            catch
            {
                await dbContext.DisposeAsync();
                throw;
            }
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
