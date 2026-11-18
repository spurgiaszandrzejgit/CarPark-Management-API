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

                entity.HasOne(s => s.Vehicle)
                    .WithMany(v => v.ParkingSessions)
                    .HasForeignKey(s => s.VehicleId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(s => s.ParkingSpace)
                    .WithMany(p => p.ParkingSessions)
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
