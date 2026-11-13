using CarPark.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Api.Data;

public static class ParkingDbSeeder
{
    public static async Task SeedAsync(CarParkDbContext context,int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        if (await context.ParkingSpaces.AnyAsync())
            return;

        for (int i = 1; i <= capacity; i++)
        {
            context.ParkingSpaces.Add(new ParkingSpace
            {
                SpaceNumber = i
            });
        }

        await context.SaveChangesAsync();
    }
}