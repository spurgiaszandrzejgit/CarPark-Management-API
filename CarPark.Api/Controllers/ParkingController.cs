
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
