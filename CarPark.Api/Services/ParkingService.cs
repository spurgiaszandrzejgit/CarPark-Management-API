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

                IsOccupied = space.ParkingSessions
                    .Any(session => session.TimeOut == null),

                VehicleReg = space.ParkingSessions
                    .Where(session => session.TimeOut == null)
                    .Select(session => session.Vehicle.VehicleReg)
                    .FirstOrDefault()
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

        var activeSession = await _context.ParkingSessions
            .AsNoTracking()
            .Where(session =>
                session.ParkingSpaceId == space.Id &&
                session.TimeOut == null)
            .Select(session => new
            {
                session.Vehicle.VehicleReg,
                session.Vehicle.VehicleType,
                session.TimeIn
            })
            .FirstOrDefaultAsync();

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