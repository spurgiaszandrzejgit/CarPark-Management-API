
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
