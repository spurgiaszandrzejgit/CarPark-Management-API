
using CarPark.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CarPark.Tests.Helpers;

public sealed class CarParkWebApplicationFactory: WebApplicationFactory<Program>
{
    private readonly string _databaseName =
        $"CarParkTest_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $@"Server=(localdb)\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Parking:Capacity"] = "3"
                });
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<CarParkDbContext>>();

            services.AddDbContext<CarParkDbContext>(options =>
                options.UseSqlServer(ConnectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Create the test database before the application starts.
        var options = new DbContextOptionsBuilder<CarParkDbContext>()
            .UseSqlServer(ConnectionString)
            .Options;

        using (var context = new CarParkDbContext(options))
        {
            context.Database.Migrate();
        }

        return base.CreateHost(builder);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            var options = new DbContextOptionsBuilder<CarParkDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            using var context = new CarParkDbContext(options);

            context.Database.EnsureDeleted();
        }
    }


}
