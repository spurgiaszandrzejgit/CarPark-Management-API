## CarPark.Api/Controllers/ParkingController.cs
``csharp

using CarPark.Api.DTOs;
using CarPark.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarPark.Api.Controllers;

[ApiController]
[Route("parking")]
public class ParkingController : ControllerBase
{
    private readonly ParkingService _parkingService;

    public ParkingController(ParkingService parkingService)
    {
        _parkingService = parkingService;
    }

    [HttpPost]
    public async Task<ActionResult<ParkVehicleResponse>> ParkVehicle([FromBody] ParkVehicleRequest request)
    {
        try
        {
            var result = await _parkingService.ParkVehicleAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<ParkingStatusResponse>> GetParkingStatus()
    {
        var result = await _parkingService.GetParkingStatusAsync();

        return Ok(result);
    }


    [HttpPost("exit")]
    public async Task<ActionResult<ExitVehicleResponse>> ExitVehicle([FromBody] ExitVehicleRequest request)
    {
        try
        {
            var result = await _parkingService.ExitVehicleAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { Message = ex.Message });
        }
    }

    [HttpGet("spaces")]
    public async Task<ActionResult<List<ParkingSpaceResponse>>> GetParkingSpaces()
    {
        var spaces = await _parkingService.GetParkingSpacesAsync();

        return Ok(spaces);
    }


    [HttpGet("spaces/{spaceNumber:int}")]
    public async Task<ActionResult<ParkingSpaceDetailsResponse>> GetParkingSpaceDetails(
    int spaceNumber)
    {
        var result = await _parkingService.GetParkingSpaceDetailsAsync(spaceNumber);

        if (result is null)
        {
            return NotFound(new
            {
                Message = "Parking space not found."
            });
        }

        return Ok(result);
    }

}

````
## CarPark.Api/DTOs/ExitVehicleRequest.cs
``csharp
namespace CarPark.Api.DTOs
{
    public class ExitVehicleRequest
    {
        public required string VehicleReg { get; set; }
    }
}

````
## CarPark.Api/DTOs/ExitVehicleResponse.cs
``csharp
namespace CarPark.Api.DTOs
{
    public class ExitVehicleResponse
    {
        public required string VehicleReg { get; set; }
        public double VehicleCharge { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime TimeOut { get; set; }
    }
}

````
## CarPark.Api/DTOs/ParkVehicleRequest.cs
``csharp
namespace CarPark.Api.DTOs
{
    public class ParkVehicleRequest
    {
        public required string VehicleReg { get; set; }
        public int VehicleType { get; set; }
    }
}

````
## CarPark.Api/DTOs/ParkVehicleResponse.cs
``csharp
namespace CarPark.Api.DTOs;

public class ParkVehicleResponse
{
    public required string VehicleReg { get; set; }
    public int SpaceNumber { get; set; }
    public DateTime TimeIn { get; set; }
}
````
## CarPark.Api/DTOs/ParkingSpaceDetailsResponse.cs
``csharp
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

````
## CarPark.Api/DTOs/ParkingSpaceResponse.cs
``csharp
namespace CarPark.Api.DTOs
{
    public class ParkingSpaceResponse
    {
        public int SpaceNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string? VehicleReg { get; set; }
    }
}

````
## CarPark.Api/DTOs/ParkingStatusResponse.cs
``csharp
namespace CarPark.Api.DTOs
{
    public class ParkingStatusResponse
    {
        public int AvailableSpaces { get; set; }
        public int OccupiedSpaces { get; set; }
    }
}

````
## CarPark.Api/Data/CarParkDbContext.cs
``csharp
using CarPark.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Api.Data
{
    public class CarParkDbContext : DbContext
    {
        public CarParkDbContext(DbContextOptions<CarParkDbContext> options) : base(options)
        {
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ParkingSpace> ParkingSpaces { get; set; }
        public DbSet<ParkingSession> ParkingSessions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(v => v.Id);

                entity.Property(v => v.VehicleReg)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.HasIndex(v => v.VehicleReg)
                    .IsUnique();
            });

            modelBuilder.Entity<ParkingSpace>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.HasIndex(p => p.SpaceNumber)
                    .IsUnique();
            });

            modelBuilder.Entity<ParkingSession>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.HasOne<Vehicle>()
                    .WithMany()
                    .HasForeignKey(s => s.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<ParkingSpace>()
                    .WithMany()
                    .HasForeignKey(s => s.ParkingSpaceId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => s.VehicleId)
                    .IsUnique()
                    .HasFilter("[TimeOut] IS NULL");

                entity.HasIndex(s => s.ParkingSpaceId)
                    .IsUnique()
                    .HasFilter("[TimeOut] IS NULL");
            });
        }
    }
}

````
## CarPark.Api/Data/ParkingDbSeeder.cs
``csharp
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
````
## CarPark.Api/Migrations/20261113081019_InitialCreate.Designer.cs
``csharp
// <auto-generated />
using System;
using CarPark.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CarPark.Api.Migrations
{
    [DbContext(typeof(CarParkDbContext))]
    [Migration("20261113081019_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CarPark.Api.Models.ParkingSession", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal?>("Charge")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ParkingSpaceId")
                        .HasColumnType("int");

                    b.Property<DateTime>("TimeIn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("TimeOut")
                        .HasColumnType("datetime2");

                    b.Property<int>("VehicleId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ParkingSpaceId")
                        .IsUnique()
                        .HasFilter("[TimeOut] IS NULL");

                    b.HasIndex("VehicleId")
                        .IsUnique()
                        .HasFilter("[TimeOut] IS NULL");

                    b.ToTable("ParkingSessions");
                });

            modelBuilder.Entity("CarPark.Api.Models.ParkingSpace", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("SpaceNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SpaceNumber")
                        .IsUnique();

                    b.ToTable("ParkingSpaces");
                });

            modelBuilder.Entity("CarPark.Api.Models.Vehicle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("VehicleReg")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("VehicleType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("VehicleReg")
                        .IsUnique();

                    b.ToTable("Vehicles");
                });

            modelBuilder.Entity("CarPark.Api.Models.ParkingSession", b =>
                {
                    b.HasOne("CarPark.Api.Models.ParkingSpace", null)
                        .WithMany()
                        .HasForeignKey("ParkingSpaceId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CarPark.Api.Models.Vehicle", null)
                        .WithMany()
                        .HasForeignKey("VehicleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

````
## CarPark.Api/Migrations/20261113081019_InitialCreate.cs
``csharp
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CarPark.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingSpaces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpaceNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSpaces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Vehicles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleReg = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleType = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vehicles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParkingSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    ParkingSpaceId = table.Column<int>(type: "int", nullable: false),
                    TimeIn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TimeOut = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Charge = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_ParkingSpaces_ParkingSpaceId",
                        column: x => x.ParkingSpaceId,
                        principalTable: "ParkingSpaces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ParkingSessions_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_ParkingSpaceId",
                table: "ParkingSessions",
                column: "ParkingSpaceId",
                unique: true,
                filter: "[TimeOut] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSessions_VehicleId",
                table: "ParkingSessions",
                column: "VehicleId",
                unique: true,
                filter: "[TimeOut] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpaces_SpaceNumber",
                table: "ParkingSpaces",
                column: "SpaceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Vehicles_VehicleReg",
                table: "Vehicles",
                column: "VehicleReg",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSessions");

            migrationBuilder.DropTable(
                name: "ParkingSpaces");

            migrationBuilder.DropTable(
                name: "Vehicles");
        }
    }
}

````
## CarPark.Api/Migrations/CarParkDbContextModelSnapshot.cs
``csharp
// <auto-generated />
using System;
using CarPark.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace CarPark.Api.Migrations
{
    [DbContext(typeof(CarParkDbContext))]
    partial class CarParkDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "10.0.15")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("CarPark.Api.Models.ParkingSession", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<decimal?>("Charge")
                        .HasColumnType("decimal(18,2)");

                    b.Property<int>("ParkingSpaceId")
                        .HasColumnType("int");

                    b.Property<DateTime>("TimeIn")
                        .HasColumnType("datetime2");

                    b.Property<DateTime?>("TimeOut")
                        .HasColumnType("datetime2");

                    b.Property<int>("VehicleId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ParkingSpaceId")
                        .IsUnique()
                        .HasFilter("[TimeOut] IS NULL");

                    b.HasIndex("VehicleId")
                        .IsUnique()
                        .HasFilter("[TimeOut] IS NULL");

                    b.ToTable("ParkingSessions");
                });

            modelBuilder.Entity("CarPark.Api.Models.ParkingSpace", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("SpaceNumber")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("SpaceNumber")
                        .IsUnique();

                    b.ToTable("ParkingSpaces");
                });

            modelBuilder.Entity("CarPark.Api.Models.Vehicle", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("VehicleReg")
                        .IsRequired()
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("VehicleType")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("VehicleReg")
                        .IsUnique();

                    b.ToTable("Vehicles");
                });

            modelBuilder.Entity("CarPark.Api.Models.ParkingSession", b =>
                {
                    b.HasOne("CarPark.Api.Models.ParkingSpace", null)
                        .WithMany()
                        .HasForeignKey("ParkingSpaceId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.HasOne("CarPark.Api.Models.Vehicle", null)
                        .WithMany()
                        .HasForeignKey("VehicleId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}

````
## CarPark.Api/Models/ParkingSession.cs
``csharp
namespace CarPark.Api.Models
{
    public class ParkingSession
    {
        public int Id { get; set; }
        public int VehicleId { get; set; }
        public int ParkingSpaceId { get; set; }
        public DateTime TimeIn { get; set; }
        public DateTime? TimeOut { get; set; }
        public decimal? Charge { get; set; }

    }
}

````
## CarPark.Api/Models/ParkingSpace.cs
``csharp
namespace CarPark.Api.Models
{
    public class ParkingSpace
    {
        public int Id { get; set; }
        public int SpaceNumber { get; set; }
    }
}

````
## CarPark.Api/Models/Vehicle.cs
``csharp
namespace CarPark.Api.Models
{
    public class Vehicle
    {
        public int Id { get; set; }
        public required string VehicleReg { get; set; }
        public VehicleType VehicleType { get; set; }
    }
}

````
## CarPark.Api/Models/VehicleType.cs
``csharp
namespace CarPark.Api.Models
{
    public enum VehicleType
    {
        Small = 1,
        Medium = 2,
        Large = 3
    }
}

````
## CarPark.Api/Program.cs
``csharp

using CarPark.Api.Data;
using CarPark.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<CarParkDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<ParkingService>();

// Allow requests from the Angular development server.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularDev", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// Apply migrations and seed parking spaces.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<CarParkDbContext>();

    await dbContext.Database.MigrateAsync();

    var capacity = builder.Configuration
        .GetValue<int>("Parking:Capacity");

    await ParkingDbSeeder.SeedAsync(dbContext, capacity);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS before authorization and controllers.
app.UseCors("AngularDev");

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }

````
## CarPark.Api/Services/ParkingChargeCalculator.cs
``csharp

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

````
## CarPark.Api/Services/ParkingService.cs
``csharp
using CarPark.Api.Data;
using CarPark.Api.DTOs;
using CarPark.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarPark.Api.Services;

public class ParkingService
{
    private readonly CarParkDbContext _context;
    private readonly TimeProvider _timeProvider;

    public ParkingService(CarParkDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<ParkVehicleResponse> ParkVehicleAsync(ParkVehicleRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.VehicleReg))
            throw new ArgumentException("Vehicle registration is required.");

        if (!Enum.IsDefined(typeof(VehicleType), request.VehicleType))
            throw new ArgumentException("Invalid vehicle type.");

        var vehicleReg = request.VehicleReg.Trim().ToUpperInvariant();

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.VehicleReg == vehicleReg);

        if (vehicle is not null)
        {
            var isAlreadyParked = await _context.ParkingSessions
                .AnyAsync(s => s.VehicleId == vehicle.Id && s.TimeOut == null);

            if (isAlreadyParked)
                throw new InvalidOperationException("Vehicle is already parked.");
        }

        var availableSpace = await _context.ParkingSpaces
            .Where(space => !_context.ParkingSessions
                .Any(session =>
                    session.ParkingSpaceId == space.Id &&
                    session.TimeOut == null))
            .OrderBy(space => space.SpaceNumber)
            .FirstOrDefaultAsync();

        if (availableSpace is null)
            throw new InvalidOperationException("No available parking spaces.");

        await using var transaction = await _context.Database.BeginTransactionAsync();

        if (vehicle is null)
        {
            vehicle = new Vehicle
            {
                VehicleReg = vehicleReg,
                VehicleType = (VehicleType)request.VehicleType
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
        }

        var timeIn = _timeProvider.GetUtcNow().UtcDateTime;

        var session = new ParkingSession
        {
            VehicleId = vehicle.Id,
            ParkingSpaceId = availableSpace.Id,
            TimeIn = timeIn
        };

        _context.ParkingSessions.Add(session);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ParkVehicleResponse
        {
            VehicleReg = vehicle.VehicleReg,
            SpaceNumber = availableSpace.SpaceNumber,
            TimeIn = timeIn
        };
    }

    public async Task<ParkingStatusResponse> GetParkingStatusAsync()
    {
        var totalSpaces = await _context.ParkingSpaces.CountAsync();

        var occupiedSpaces = await _context.ParkingSessions
            .CountAsync(s => s.TimeOut == null);

        return new ParkingStatusResponse
        {
            AvailableSpaces = totalSpaces - occupiedSpaces,
            OccupiedSpaces = occupiedSpaces
        };
    }


    public async Task<ExitVehicleResponse> ExitVehicleAsync(ExitVehicleRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (string.IsNullOrWhiteSpace(request.VehicleReg))
            throw new ArgumentException("Vehicle registration is required.");

        var vehicleReg = request.VehicleReg.Trim().ToUpperInvariant();

        var vehicle = await _context.Vehicles
            .FirstOrDefaultAsync(v => v.VehicleReg == vehicleReg);

        if (vehicle is null)
            throw new InvalidOperationException("Vehicle not found.");

        var session = await _context.ParkingSessions
            .FirstOrDefaultAsync(s =>
                s.VehicleId == vehicle.Id &&
                s.TimeOut == null);

        if (session is null)
            throw new InvalidOperationException(
                "Vehicle is not currently parked.");

        var timeOut = _timeProvider.GetUtcNow().UtcDateTime;
        var duration = timeOut - session.TimeIn;

        var charge = ParkingChargeCalculator.Calculate(
            vehicle.VehicleType,
            duration);

        session.TimeOut = timeOut;
        session.Charge = charge;

        await _context.SaveChangesAsync();

        return new ExitVehicleResponse
        {
            VehicleReg = vehicle.VehicleReg,
            VehicleCharge = (double)charge,
            TimeIn = session.TimeIn,
            TimeOut = timeOut
        };
    }

    public async Task<List<ParkingSpaceResponse>> GetParkingSpacesAsync()
    {
        return await _context.ParkingSpaces
            .AsNoTracking()
            .OrderBy(space => space.SpaceNumber)
            .Select(space => new ParkingSpaceResponse
            {
                SpaceNumber = space.SpaceNumber,

                IsOccupied = _context.ParkingSessions
                    .Any(session =>
                        session.ParkingSpaceId == space.Id &&
                        session.TimeOut == null),

                VehicleReg = (
                        from session in _context.ParkingSessions
                        join vehicle in _context.Vehicles
                            on session.VehicleId equals vehicle.Id
                        where session.ParkingSpaceId == space.Id
                              && session.TimeOut == null
                        select vehicle.VehicleReg
                    ).FirstOrDefault()
            })
            .ToListAsync();
    }

    public async Task<ParkingSpaceDetailsResponse?> GetParkingSpaceDetailsAsync(int spaceNumber)
    {
        var space = await _context.ParkingSpaces
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.SpaceNumber == spaceNumber);

        if (space is null)
            return null;

        var activeSession = await (
            from session in _context.ParkingSessions.AsNoTracking()
            join vehicle in _context.Vehicles.AsNoTracking()
                on session.VehicleId equals vehicle.Id
            where session.ParkingSpaceId == space.Id
                  && session.TimeOut == null
            select new
            {
                vehicle.VehicleReg,
                vehicle.VehicleType,
                session.TimeIn
            }
        ).FirstOrDefaultAsync();

        if (activeSession is null)
        {
            return new ParkingSpaceDetailsResponse
            {
                SpaceNumber = space.SpaceNumber,
                IsOccupied = false
            };
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var duration = now - activeSession.TimeIn;

        var currentCharge = ParkingChargeCalculator.Calculate(
            activeSession.VehicleType,
            duration);

        return new ParkingSpaceDetailsResponse
        {
            SpaceNumber = space.SpaceNumber,
            IsOccupied = true,
            VehicleReg = activeSession.VehicleReg,
            VehicleType = (int)activeSession.VehicleType,
            TimeIn = activeSession.TimeIn,
            CurrentCharge = currentCharge
        };
    }
}
````
## CarPark.Tests/Controllers/ParkingApiTests.cs
``csharp

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

````
## CarPark.Tests/Helpers/CarParkWebApplicationFactory.cs
``csharp

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

````
## CarPark.Tests/Helpers/ParkingTestContext.cs
``csharp

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

````
## CarPark.Tests/ParkingChargeCalculatorTests.cs
``csharp

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

````
## CarPark.Tests/ParkingServiceTests.cs
``csharp

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

````
## CarPark.Tests/ParkingSpacesEndpointTests.cs
``csharp

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

````
## carpark-ui/src/app/app.config.ts
``typescript
import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient()
  ]
};

````
## carpark-ui/src/app/app.html
``html

<main class="dashboard">
  <header class="dashboard-header">
    <div>
      <p class="subtitle">Parking Management System</p>
      <h1>CarPark Dashboard</h1>
      <p>Real-time parking availability</p>
    </div>

    <button
      type="button"
      (click)="refreshParking()"
      [disabled]="loading()">
      {{ loading() ? 'Loading...' : 'Refresh' }}
    </button>
  </header>

  @if (error()) {
    <div class="error-message" role="alert">
      {{ error() }}
    </div>
  }

  @if (parkingStatus(); as status) {
    <section class="stats">
      <article class="stat-card">
        <span>Available Spaces</span>
        <strong>{{ status.availableSpaces }}</strong>
        <p>Ready for parking</p>
      </article>

      <article class="stat-card">
        <span>Occupied Spaces</span>
        <strong>{{ status.occupiedSpaces }}</strong>
        <p>Currently in use</p>
      </article>

      <article class="stat-card">
        <span>Total Capacity</span>
        <strong>
          {{ status.availableSpaces + status.occupiedSpaces }}
        </strong>
        <p>Parking spaces</p>
      </article>
    </section>


    <app-parking-lot
      (spaceSelected)="selectSpace($event)"
    />

    @if (selectedSpaceNumber(); as spaceNumber) {
      <div class="drawer-backdrop" (click)="closeSpaceDetails()"></div>

      <aside
        class="parking-drawer"
        role="dialog"
        aria-modal="true"
        aria-label="Parking space details"
      >
      <app-parking-space-details
        [spaceNumber]="spaceNumber"
        (closed)="closeSpaceDetails()"
        (parkingChanged)="refreshParking()"
        (operationCompleted)="showNotification($event)"
      />
      </aside>
    }


  } @else if (loading()) {
    <p>Loading parking information...</p>
  }
</main>

@if (notification(); as message) {
  <div class="toast" role="status" aria-live="polite">
    <span class="toast__icon">✓</span>

    <span class="toast__message">
      {{ message }}
    </span>

    <button
      type="button"
      class="toast__close"
      aria-label="Dismiss notification"
      (click)="dismissNotification()"
    >
      ×
    </button>
  </div>
}


````
## carpark-ui/src/app/app.routes.ts
``typescript
import { Routes } from '@angular/router';

export const routes: Routes = [];

````
## carpark-ui/src/app/app.scss
``scss

.dashboard {
  max-width: 1100px;
  margin: 0 auto;
  padding: 48px 24px;
  font-family: Inter, Arial, sans-serif;
}

.dashboard-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 24px;
  margin-bottom: 36px;

  h1 {
    font-size: 36px;
    margin: 8px 0;
  }

  p {
    color: #64748b;
  }

  button {
    padding: 12px 22px;
    border: none;
    border-radius: 8px;
    background: #2563eb;
    color: white;
    cursor: pointer;

    &:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }
  }
}

.subtitle {
  font-size: 13px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.stats {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 20px;
}

.stat-card {
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;
  padding: 28px;

  span {
    color: #64748b;
    font-size: 14px;
  }

  strong {
    display: block;
    font-size: 44px;
    margin: 18px 0;
    color: #0f172a;
  }

  p {
    color: #64748b;
    font-size: 13px;
  }
}

.error-message {
  padding: 16px;
  margin-bottom: 20px;
  border-radius: 8px;
  background: #fee2e2;
  color: #991b1b;
}


.drawer-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1000;
  background: rgba(15, 23, 42, 0.45);
  backdrop-filter: blur(3px);
}

.parking-drawer {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  z-index: 1001;

  width: min(460px, 100%);
  box-sizing: border-box;
  overflow-y: auto;

  background: #fff;
  box-shadow: -12px 0 40px rgba(15, 23, 42, 0.15);

  animation: drawer-slide-in 0.25s ease-out;

  app-parking-space-details {
    display: block;
    min-height: 100%;
  }
}

@keyframes drawer-slide-in {
  from {
    transform: translateX(100%);
  }

  to {
    transform: translateX(0);
  }
}

@media (max-width: 520px) {
  .parking-drawer {
    width: 100%;
  }
}

@media (prefers-reduced-motion: reduce) {
  .parking-drawer {
    animation: none;
  }
}


@media (max-width: 700px) {
  .dashboard-header {
    align-items: flex-start;
    flex-direction: column;
  }

  .stats {
    grid-template-columns: 1fr;
  }
}


.toast {
  position: fixed;
  right: 24px;
  bottom: 24px;
  z-index: 1100;

  display: flex;
  align-items: center;
  gap: 12px;

  box-sizing: border-box;
  width: max-content;
  max-width: min(420px, calc(100vw - 32px));
  padding: 16px 18px;

  background: #0f172a;
  color: #fff;
  border-radius: 12px;
  box-shadow: 0 12px 32px rgba(15, 23, 42, 0.2);

  animation: toast-enter 0.2s ease-out;

  &__icon {
    display: grid;
    place-items: center;
    flex-shrink: 0;

    width: 26px;
    height: 26px;

    border-radius: 50%;
    background: #16a34a;
    color: #fff;
    font-size: 16px;
    font-weight: 700;
  }

  &__message {
    flex: 1;
    font-size: 14px;
    line-height: 1.5;
  }

  &__close {
    flex-shrink: 0;
    border: none;
    background: transparent;
    color: #cbd5e1;
    font-size: 22px;
    cursor: pointer;

    &:hover {
      color: #fff;
    }
  }
}

@keyframes toast-enter {
  from {
    opacity: 0;
    transform: translateY(12px);
  }

  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@media (max-width: 520px) {
  .toast {
    left: 16px;
    right: 16px;
    bottom: 16px;
    width: auto;
    max-width: none;
  }
}

@media (prefers-reduced-motion: reduce) {
  .toast {
    animation: none;
  }
}


````
## carpark-ui/src/app/app.ts
``typescript

import {
  Component,
  inject,
  OnInit,
  OnDestroy,
  signal,
  viewChild
} from '@angular/core';

import {
  ParkingApiService,
  ParkingStatus
} from './core/services/parking-api';

import { ParkingLot } from './features/parking/parking-lot/parking-lot';
import { ParkingSpaceDetails } from './features/parking/parking-space-details/parking-space-details';

@Component({
  selector: 'app-root',
  imports: [
    ParkingLot,
    ParkingSpaceDetails
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  private readonly parkingApi = inject(ParkingApiService);

  readonly parkingStatus = signal<ParkingStatus | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly parkingLot = viewChild(ParkingLot);
  readonly selectedSpaceNumber = signal<number | null>(null);
  readonly notification = signal<string | null>(null);

  private notificationTimeout: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.loadParkingStatus();
  }

  loadParkingStatus(): void {
    this.loading.set(true);
    this.error.set(null);

    this.parkingApi.getStatus().subscribe({
      next: status => {
        this.parkingStatus.set(status);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Unable to connect to the parking API.');
        this.loading.set(false);
      }
    });
  }

  refreshParking(): void {
    this.loadParkingStatus();
    this.parkingLot()?.loadSpaces();
  }

  selectSpace(spaceNumber: number): void {
    this.selectedSpaceNumber.set(spaceNumber);
  }

  closeSpaceDetails(): void {
    this.selectedSpaceNumber.set(null);
  }

  showNotification(message: string): void {
    this.dismissNotification();

    this.notification.set(message);

    this.notificationTimeout = setTimeout(() => {
      this.notification.set(null);
      this.notificationTimeout = null;
    }, 5000);
  }

  dismissNotification(): void {
    if (this.notificationTimeout !== null) {
      clearTimeout(this.notificationTimeout);
      this.notificationTimeout = null;
    }

    this.notification.set(null);
  }

  ngOnDestroy(): void {
    this.dismissNotification();
  }
}

````
## carpark-ui/src/app/core/services/parking-api.ts
``typescript
import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environment/environment';

export interface ParkingStatus {
  availableSpaces: number;
  occupiedSpaces: number;
}

@Injectable({
  providedIn: 'root'
})
export class ParkingApiService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/parking`;

  getStatus(): Observable<ParkingStatus> {
    return this.http.get<ParkingStatus>(this.apiUrl);
  }

  parkVehicle(request: ParkVehicleRequest): Observable<ParkVehicleResponse> {
    return this.http.post<ParkVehicleResponse>(
      this.apiUrl,
      request
    );
  }

  exitVehicle(request: ExitVehicleRequest): Observable<ExitVehicleResponse> {
    return this.http.post<ExitVehicleResponse>(
      `${this.apiUrl}/exit`,
      request
    );
  }

  getSpaces(): Observable<ParkingSpace[]> {
    return this.http.get<ParkingSpace[]>(
      `${environment.apiUrl}/parking/spaces`
    );
  }

  getSpaceDetails(spaceNumber: number): Observable<ParkingSpaceDetails> {
    return this.http.get<ParkingSpaceDetails>(
      `${environment.apiUrl}/parking/spaces/${spaceNumber}`
    );
  }
}

export interface ParkVehicleRequest {
  vehicleReg: string;
  vehicleType: number;
}

export interface ParkVehicleResponse {
  vehicleReg: string;
  spaceNumber: number;
  timeIn: string;
}

export interface ExitVehicleRequest {
  vehicleReg: string;
}

export interface ExitVehicleResponse {
  vehicleReg: string;
  vehicleCharge: number;
  timeIn: string;
  timeOut: string;
}

export interface ParkingSpace {
  spaceNumber: number;
  isOccupied: boolean;
  vehicleReg: string | null;
}

export interface ParkingSpaceDetails {
  spaceNumber: number;
  isOccupied: boolean;
  vehicleReg: string | null;
  vehicleType: number | null;
  timeIn: string | null;
  currentCharge: number | null;
}
````
## carpark-ui/src/app/core/utils/api-error.ts
``typescript
import { HttpErrorResponse } from '@angular/common/http';

export function getApiErrorMessage(error: HttpErrorResponse): string {
  if (error.status === 0) {
    return 'Unable to connect to the server.';
  }

  if (error.status === 409) {
    return 'The operation conflicts with the current parking state.';
  }

  if (error.status === 400) {
    return 'Invalid request. Please check the vehicle registration.';
  }

  if (error.status >= 500) {
    return 'A server error occurred. Please try again later.';
  }

  return 'Unable to process the request.';
}
````
## carpark-ui/src/app/features/parking/exit-vehicle/exit-vehicle.html
``html

<section class="parking-form">
  <h2>Vehicle Exit</h2>
  <p>Check out a vehicle and calculate the parking charge.</p>

  <form [formGroup]="form" (ngSubmit)="submit()">
    <div class="form-field">
      <label for="exitVehicleReg">Vehicle Registration</label>

      <input
        id="exitVehicleReg"
        type="text"
        formControlName="vehicleReg"
        placeholder="e.g. WA12345"
      />

      @if (form.controls.vehicleReg.touched &&
           form.controls.vehicleReg.invalid) {
        <small class="validation-error">
          Vehicle registration is required.
        </small>
      }
    </div>

    <button type="submit" [disabled]="loading()">
      {{ loading() ? 'Processing...' : 'Exit Vehicle' }}
    </button>
  </form>

  @if (error()) {
    <div class="error-message" role="alert">
      {{ error() }}
    </div>
  }

  @if (result(); as exit) {
    <div class="success-message" role="status">
      <strong>Vehicle exited successfully!</strong>

      <p>Registration: {{ exit.vehicleReg }}</p>

      <p>
        Time in:
        {{ exit.timeIn | date:'dd/MM/yyyy HH:mm:ss' }}
      </p>

      <p>
        Time out:
        {{ exit.timeOut | date:'dd/MM/yyyy HH:mm:ss' }}
      </p>

      <h3>
        Total Charge:
        {{ exit.vehicleCharge | currency:'GBP' }}
      </h3>
    </div>
  }
</section>

````
## carpark-ui/src/app/features/parking/exit-vehicle/exit-vehicle.scss
``scss

.parking-form {
  margin-top: 32px;
  padding: 28px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;

  h2 {
    margin: 0 0 8px;
  }

  > p {
    color: #64748b;
    margin-bottom: 24px;
  }
}

form {
  display: flex;
  flex-direction: column;
  gap: 18px;
  max-width: 420px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 8px;

  label {
    font-weight: 600;
    font-size: 14px;
  }

  input,
  select {
    padding: 12px;
    border: 1px solid #cbd5e1;
    border-radius: 8px;
    font: inherit;
  }
}

button {
  padding: 12px 20px;
  background: #2563eb;
  color: white;
  border: 0;
  border-radius: 8px;
  cursor: pointer;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.validation-error {
  color: #b91c1c;
}

.error-message,
.success-message {
  margin-top: 20px;
  padding: 16px;
  border-radius: 8px;
}

.error-message {
  background: #fee2e2;
  color: #991b1b;
}

.success-message {
  background: #dcfce7;
  color: #166534;
}

````
## carpark-ui/src/app/features/parking/exit-vehicle/exit-vehicle.ts
``typescript

import { DatePipe, CurrencyPipe } from '@angular/common';
import { Component, inject, output, signal } from '@angular/core';
import {FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  ParkingApiService,
  ExitVehicleResponse
} from '../../../core/services/parking-api';

import { getApiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-exit-vehicle',
  imports: [ReactiveFormsModule, DatePipe, CurrencyPipe],
  templateUrl: './exit-vehicle.html',
  styleUrl: './exit-vehicle.scss'
})
export class ExitVehicle {
  private readonly parkingApi = inject(ParkingApiService);

  readonly exited = output<void>();
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ExitVehicleResponse | null>(null);

  readonly form = new FormGroup({
    vehicleReg: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.parkingApi.exitVehicle(this.form.getRawValue()).subscribe({
      next: response => {
        this.result.set(response);
        this.loading.set(false);
        this.form.reset();
        this.exited.emit();
      },
      error: err => {
        this.error.set(getApiErrorMessage(err));
        this.loading.set(false);
      }
    });
  }
}

````
## carpark-ui/src/app/features/parking/park-vehicle/park-vehicle.html
``html

<section class="parking-form">
  <h2>Vehicle Entry</h2>
  <p>Register a vehicle and assign a parking space.</p>

  <form [formGroup]="form" (ngSubmit)="submit()">
    <div class="form-field">
      <label for="vehicleReg">Vehicle Registration</label>
      <input
        id="vehicleReg"
        type="text"
        formControlName="vehicleReg"
        placeholder="e.g. WA12345"
        maxlength="20"
      />

      @if (form.controls.vehicleReg.touched &&
           form.controls.vehicleReg.invalid) {
        <small class="validation-error">
          Vehicle registration is required.
        </small>
      }
    </div>

    <div class="form-field">
      <label for="vehicleType">Vehicle Type</label>
      <select id="vehicleType" formControlName="vehicleType">
        <option [ngValue]="1">Small — £0.10/min</option>
        <option [ngValue]="2">Medium — £0.20/min</option>
        <option [ngValue]="3">Large — £0.40/min</option>
      </select>
    </div>

    <button type="submit" [disabled]="loading()">
      {{ loading() ? 'Parking...' : 'Park Vehicle' }}
    </button>
  </form>

  @if (error()) {
    <div class="error-message" role="alert">
      {{ error() }}
    </div>
  }

  @if (result(); as parking) {
    <div class="success-message" role="status">
      <strong>Vehicle parked successfully!</strong>
      <p>Registration: {{ parking.vehicleReg }}</p>
      <p>Assigned space: {{ parking.spaceNumber }}</p>
      <p>Time in: {{ parking.timeIn | date:'dd/MM/yyyy HH:mm:ss' }}</p>
    </div>
  }
</section>

````
## carpark-ui/src/app/features/parking/park-vehicle/park-vehicle.scss
``scss

.parking-form {
  margin-top: 32px;
  padding: 28px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 14px;

  h2 {
    margin: 0 0 8px;
  }

  > p {
    color: #64748b;
    margin-bottom: 24px;
  }
}

form {
  display: flex;
  flex-direction: column;
  gap: 18px;
  max-width: 420px;
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 8px;

  label {
    font-weight: 600;
    font-size: 14px;
  }

  input,
  select {
    padding: 12px;
    border: 1px solid #cbd5e1;
    border-radius: 8px;
    font: inherit;
  }
}

button {
  padding: 12px 20px;
  background: #2563eb;
  color: white;
  border: 0;
  border-radius: 8px;
  cursor: pointer;

  &:disabled {
    opacity: 0.5;
    cursor: not-allowed;
  }
}

.validation-error {
  color: #b91c1c;
}

.error-message,
.success-message {
  margin-top: 20px;
  padding: 16px;
  border-radius: 8px;
}

.error-message {
  background: #fee2e2;
  color: #991b1b;
}

.success-message {
  background: #dcfce7;
  color: #166534;
}

````
## carpark-ui/src/app/features/parking/park-vehicle/park-vehicle.ts
``typescript

import { Component, inject, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import {
  ParkingApiService,
  ParkVehicleResponse
} from '../../../core/services/parking-api';
import { getApiErrorMessage } from '../../../core/utils/api-error';

@Component({
  selector: 'app-park-vehicle',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './park-vehicle.html',
  styleUrl: './park-vehicle.scss'
})
export class ParkVehicle {
  private readonly parkingApi = inject(ParkingApiService);

  readonly parked = output<void>();

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ParkVehicleResponse | null>(null);

  readonly form = new FormGroup({
    vehicleReg: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required]
    }),
    vehicleType: new FormControl(1, {
      nonNullable: true,
      validators: [Validators.required]
    })
  });

  submit(): void {
    if (this.form.invalid || this.loading()) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.parkingApi.parkVehicle(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.result.set(response);
        this.loading.set(false);
        this.form.reset();
        this.parked.emit();
      },
      error: err => {
        this.error.set(getApiErrorMessage(err));
        this.loading.set(false);
      }
    });
  }
}

````
## carpark-ui/src/app/features/parking/parking-lot/parking-lot.html
``html

<section class="parking-lot">
  <header class="parking-lot__header">
    <div>
      <h2>Parking Lot</h2>
      <p>Live parking availability</p>
    </div>

    <button type="button" (click)="loadSpaces()" [disabled]="loading()">
      Refresh
    </button>
  </header>

  @if (loading()) {
    <p class="state-message">Loading parking spaces...</p>
  } @else if (error()) {
    <p class="state-message state-message--error">{{ error() }}</p>
  } @else {
    <div class="parking-grid">
      @for (space of spaces(); track space.spaceNumber) {
            <button
            type="button"
            class="parking-space"
            [class.parking-space--occupied]="space.isOccupied"
            (click)="selectSpace(space.spaceNumber)"
            >

            <span class="parking-space__number">
                P{{ space.spaceNumber.toString().padStart(2, '0') }}
            </span>

            <div class="parking-space__icon">
                <svg viewBox="0 0 64 64" aria-hidden="true">
                <path
                    fill="currentColor"
                    d="M18 14h28c3 0 5 2 6 5l5 14v18c0 2-2 4-4 4h-5v-6H16v6h-5c-2 0-4-2-4-4V33l5-14c1-3 3-5 6-5zm1 7-4 12h34l-4-12H19zm-3 18a4 4 0 1 0 0 8 4 4 0 0 0 0-8zm32 0a4 4 0 1 0 0 8 4 4 0 0 0 0-8z"
                />
                </svg>
            </div>

            <span class="parking-space__status">
                {{ space.isOccupied ? 'Occupied' : 'Available' }}
            </span>

          @if (space.vehicleReg) {
            <span class="parking-space__registration">
              {{ space.vehicleReg }}
            </span>
          }
        </button>
      } @empty {
        <p class="state-message">No parking spaces configured.</p>
      }
    </div>
  }
</section>

````
## carpark-ui/src/app/features/parking/parking-lot/parking-lot.scss
``scss

.parking-lot {
  padding: 24px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  box-shadow: 0 4px 20px rgba(15, 23, 42, 0.04);

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 24px;

    h2 {
      margin: 0;
      color: #0f172a;
      font-size: 22px;
    }

    p {
      margin: 6px 0 0;
      color: #64748b;
      font-size: 14px;
    }

    button {
      padding: 10px 18px;
      border: 1px solid #cbd5e1;
      border-radius: 8px;
      background: #fff;
      cursor: pointer;
      font-weight: 600;

      &:hover:not(:disabled) {
        background: #f1f5f9;
      }

      &:disabled {
        opacity: 0.5;
        cursor: not-allowed;
      }
    }
  }
}

.parking-grid {
  display: grid;
  grid-template-columns: repeat(5, minmax(0, 1fr));
  gap: 14px;
}

.parking-space {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 10px;

  width: 100%;
  min-height: 150px;
  padding: 14px 6px;

  border: 2px solid #bbf7d0;
  border-radius: 12px;
  background: #f0fdf4;
  color: #16a34a;

  font: inherit;
  text-align: center;
  appearance: none;
  cursor: pointer;

  transition:
    transform 0.2s ease,
    box-shadow 0.2s ease,
    border-color 0.2s ease;

  &:hover {
    transform: translateY(-3px);
    border-color: currentColor;
    box-shadow: 0 6px 16px rgba(15, 23, 42, 0.08);
  }

  &:focus-visible {
    outline: 3px solid #2563eb;
    outline-offset: 3px;
  }

  &:active {
    transform: scale(0.98);
  }

  &__number {
    font-size: 12px;
    font-weight: 700;
    color: #64748b;
  }

  &__icon svg {
    width: 52px;
    height: 52px;
  }

  &__status {
    font-size: 13px;
    font-weight: 700;
  }

  &__registration {
    font-size: 11px;
    font-weight: 600;
    color: #334155;
    overflow-wrap: anywhere;
    text-align: center;
  }

  &--occupied {
    border-color: #fcd34d;
    background: #fffbeb;
    color: #d97706;
  }
}

.state-message {
  padding: 24px;
  color: #64748b;
  text-align: center;

  &--error {
    color: #dc2626;
  }
}

@media (max-width: 900px) {
  .parking-grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
  }
}

@media (max-width: 520px) {
  .parking-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .parking-lot {
    padding: 16px;
  }
}

@media (prefers-reduced-motion: reduce) {
  .parking-space {
    transition: none;
  }
}

````
## carpark-ui/src/app/features/parking/parking-lot/parking-lot.ts
``typescript
import { Component, OnInit, inject, output, signal } from '@angular/core';
import {
  ParkingApiService,
  ParkingSpace
} from '../../../core/services/parking-api';

@Component({
  selector: 'app-parking-lot',
  standalone: true,
  imports: [],
  templateUrl: './parking-lot.html',
  styleUrl: './parking-lot.scss'
})
export class ParkingLot implements OnInit {
  private readonly parkingApi = inject(ParkingApiService);

  readonly spaces = signal<ParkingSpace[]>([]);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly spaceSelected = output<number>();

  ngOnInit(): void {
    this.loadSpaces();
  }

  loadSpaces(): void {
    this.loading.set(true);
    this.error.set(null);

    this.parkingApi.getSpaces().subscribe({
      next: spaces => {
        this.spaces.set(spaces);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load parking spaces.');
        this.loading.set(false);
      }
    });
  }

  selectSpace(spaceNumber: number): void {
    this.spaceSelected.emit(spaceNumber);
  }
}
````
## carpark-ui/src/app/features/parking/parking-space-details/parking-space-details.html
``html

<section class="space-details">
  <header class="space-details__header">
    <div>
      <span class="space-details__label">PARKING SPACE</span>
      <h2>P{{ spaceNumber().toString().padStart(2, '0') }}</h2>
    </div>

    <button type="button" class="back-button" (click)="close()">
      Back
    </button>
  </header>

  @if (success()) {
    <p class="message message--success">{{ success() }}</p>
  }

  @if (error()) {
    <p class="message message--error">{{ error() }}</p>
  }

  @if (loading()) {
    <p class="state-message">Loading parking details...</p>
  } @else if (details(); as space) {
    @if (space.isOccupied) {
      <div class="session-card">
        <span class="status status--occupied">Occupied</span>

        <h3>{{ space.vehicleReg }}</h3>

        <div class="detail-row">
          <span>Vehicle type</span>
          <strong>
            @switch (space.vehicleType) {
              @case (1) { Small }
              @case (2) { Medium }
              @case (3) { Large }
              @default { Unknown }
            }
          </strong>
        </div>

        <div class="detail-row">
          <span>Entry time</span>
          <strong>{{ space.timeIn | date:'dd MMM yyyy, HH:mm:ss' }}</strong>
        </div>

        <div class="charge">
          <span>Current charge</span>
          <strong>{{ space.currentCharge | currency:'GBP' }}</strong>
        </div>

        <button
          type="button"
          class="action-button action-button--exit"
          [disabled]="submitting()"
          (click)="exitVehicle()"
        >
          {{ submitting() ? 'Processing...' : 'Exit Vehicle' }}
        </button>
      </div>
    } @else {
      <div class="session-card">
        <span class="status status--available">Available</span>
        <h3>Park a vehicle</h3>

        <p class="hint">
          Parking spaces are assigned automatically.
          The system will select the first available space,
          regardless of which space you clicked.
        </p>

        <form [formGroup]="parkForm" (ngSubmit)="parkVehicle()">
          <div class="form-field">
            <label for="vehicleReg">Vehicle registration</label>
            <input
              id="vehicleReg"
              type="text"
              formControlName="vehicleReg"
              placeholder="e.g. WA12345"
            />

            @if (parkForm.controls.vehicleReg.touched &&
                 parkForm.controls.vehicleReg.invalid) {
              <small>Enter a valid registration number.</small>
            }
          </div>

          <div class="form-field">
            <label for="vehicleType">Vehicle type</label>
            <select id="vehicleType" formControlName="vehicleType">
              <option [ngValue]="1">Small</option>
              <option [ngValue]="2">Medium</option>
              <option [ngValue]="3">Large</option>
            </select>
          </div>

          <button
            type="submit"
            class="action-button action-button--park"
            [disabled]="submitting()"
          >
            {{ submitting() ? 'Processing...' : 'Park Vehicle' }}
          </button>
        </form>
      </div>
    }
  }
</section>

````
## carpark-ui/src/app/features/parking/parking-space-details/parking-space-details.scss
``scss

.space-details {
  min-height: 100%;
  box-sizing: border-box;
  padding: 28px 24px;
  background: #fff;
  border: none;
  border-radius: 0;
  box-shadow: none;

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 16px;
    margin-bottom: 24px;

    h2 {
      margin: 6px 0 0;
      color: #0f172a;
      font-size: 26px;
    }
  }

  &__label {
    color: #94a3b8;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 1.5px;
  }
}

.back-button {
  padding: 10px 18px;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  background: #fff;
  color: #334155;
  font-weight: 600;
  cursor: pointer;

  &:hover {
    background: #f1f5f9;
  }
}

.session-card {
  padding: 24px;
  background: #f8fafc;
  border-radius: 12px;

  h3 {
    margin: 16px 0 24px;
    color: #0f172a;
    font-size: 24px;
  }

  p {
    color: #475569;
    line-height: 1.6;
  }
}

.status {
  display: inline-block;
  font-size: 13px;
  font-weight: 700;

  &--occupied {
    color: #d97706;
  }

  &--available {
    color: #16a34a;
  }
}

.detail-row {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  padding: 14px 0;
  border-bottom: 1px solid #e2e8f0;

  span {
    color: #64748b;
  }

  strong {
    color: #0f172a;
    text-align: right;
  }
}

.charge {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 16px;
  margin-top: 24px;
  padding: 18px;
  border-radius: 10px;
  background: #ecfdf5;

  span {
    color: #047857;
    font-weight: 600;
  }

  strong {
    color: #065f46;
    font-size: 26px;
  }
}

.hint {
  padding: 14px;
  background: #eff6ff;
  border-radius: 8px;
  font-size: 13px;
}

.state-message {
  padding: 24px;
  color: #64748b;
  text-align: center;

  &--error {
    color: #dc2626;
  }
}

@media (max-width: 520px) {
  .space-details {
    padding: 16px;
  }

  .detail-row,
  .charge {
    flex-wrap: wrap;
  }
}
.message {
  padding: 14px 16px;
  margin-bottom: 18px;
  border-radius: 8px;
  font-weight: 600;

  &--success {
    background: #ecfdf5;
    color: #047857;
  }

  &--error {
    background: #fef2f2;
    color: #dc2626;
  }
}

.form-field {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin: 18px 0;

  label {
    color: #334155;
    font-size: 14px;
    font-weight: 600;
  }

  input,
  select {
    width: 100%;
    box-sizing: border-box;
    padding: 12px 14px;
    border: 1px solid #cbd5e1;
    border-radius: 8px;
    background: #fff;
    font: inherit;

    &:focus {
      outline: 2px solid #2563eb;
      outline-offset: 1px;
    }
  }

  small {
    color: #dc2626;
  }
}

.action-button {
  width: 100%;
  padding: 14px 20px;
  margin-top: 20px;
  border: none;
  border-radius: 10px;
  color: #fff;
  font: inherit;
  font-weight: 700;
  cursor: pointer;

  &--park {
    background: #16a34a;

    &:hover:not(:disabled) {
      background: #15803d;
    }
  }

  &--exit {
    background: #dc2626;

    &:hover:not(:disabled) {
      background: #b91c1c;
    }
  }

  &:disabled {
    opacity: 0.6;
    cursor: not-allowed;
  }
}
````
## carpark-ui/src/app/features/parking/parking-space-details/parking-space-details.ts
``typescript

import {
  Component,
  inject,
  input,
  output,
  signal,
  OnInit
} from '@angular/core';

import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';

import {
  ParkingApiService,
  ParkingSpaceDetails as ParkingSpaceDetailsDto
} from '../../../core/services/parking-api';

@Component({
  selector: 'app-parking-space-details',
  standalone: true,
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule],
  templateUrl: './parking-space-details.html',
  styleUrl: './parking-space-details.scss'
})
export class ParkingSpaceDetails implements OnInit {
  private readonly parkingApi = inject(ParkingApiService);
  private readonly fb = inject(FormBuilder);

  readonly spaceNumber = input.required<number>();
  readonly closed = output<void>();
  readonly parkingChanged = output<void>();

  readonly details = signal<ParkingSpaceDetailsDto | null>(null);
  readonly loading = signal(false);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);
  readonly operationCompleted = output<string>();

  readonly parkForm = this.fb.nonNullable.group({
    vehicleReg: ['', [Validators.required, Validators.maxLength(20)]],
    vehicleType: [1, Validators.required]
  });

  ngOnInit(): void {
    this.loadDetails();
  }

  loadDetails(): void {
    this.loading.set(true);
    this.error.set(null);

    this.parkingApi.getSpaceDetails(this.spaceNumber()).subscribe({
      next: details => {
        this.details.set(details);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Failed to load parking space details.');
        this.loading.set(false);
      }
    });
  }


  parkVehicle(): void {
    if (this.parkForm.invalid || this.submitting()) {
      this.parkForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.success.set(null);

    const request = this.parkForm.getRawValue();

    this.parkingApi.parkVehicle(request).subscribe({
      next: response => {
        this.submitting.set(false);

        this.operationCompleted.emit(
          `Vehicle ${response.vehicleReg} parked in P${String(response.spaceNumber).padStart(2, '0')}.`
        );

        this.parkingChanged.emit();
        this.closed.emit();
      },
      error: (err: HttpErrorResponse) => {
        console.error('Parking failed:', err);

        const apiError = err.error;

        const message =
          typeof apiError === 'string'
            ? apiError
            : apiError?.message ??
              apiError?.detail ??
              'Unable to park vehicle. Please try again.';

        this.error.set(message);
        this.submitting.set(false);
      }
    });
  }

  exitVehicle(): void {
    const vehicleReg = this.details()?.vehicleReg;

    if (!vehicleReg || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.error.set(null);
    this.success.set(null);

    this.parkingApi.exitVehicle({ vehicleReg }).subscribe({
      next: response => {
        this.submitting.set(false);

        this.operationCompleted.emit(
          `Vehicle ${response.vehicleReg} exited. Total charge: £${response.vehicleCharge.toFixed(2)}.`
        );

        this.parkingChanged.emit();
        this.closed.emit();
      },
      error: (err: HttpErrorResponse) => {
        console.error('Exit vehicle failed:', err);

        const apiError = err.error;

        const message =
          typeof apiError === 'string'
            ? apiError
            : apiError?.message ??
              apiError?.detail ??
              'Unable to exit vehicle. Please try again.';

        this.error.set(message);
        this.submitting.set(false);
      }
    });
  }


  close(): void {
    this.closed.emit();
  }
}

````
## carpark-ui/src/environment/environment.ts
``typescript
export const environment = {
  apiUrl: 'https://localhost:7030'
};
````
## carpark-ui/src/index.html
``html
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>CarparkUi</title>
  <base href="/">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <link rel="icon" type="image/x-icon" href="favicon.ico">
</head>
<body>
  <app-root></app-root>
</body>
</html>

````
## carpark-ui/src/main.ts
``typescript
import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';

bootstrapApplication(App, appConfig)
  .catch((err) => console.error(err));

````
## carpark-ui/src/styles.scss
``scss
/* You can add global styles to this file, and also import other style files */

````
