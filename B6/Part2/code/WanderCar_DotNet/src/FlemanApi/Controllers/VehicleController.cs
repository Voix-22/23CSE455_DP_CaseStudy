using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

[ApiController]
[Route("api")]
[AllowAnonymous]
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _vehicleService;

    public VehicleController(IVehicleService vehicleService)
    {
        _vehicleService = vehicleService;
    }

    [HttpGet("car-types")]
    public async Task<IActionResult> GetCarTypes(
        [FromQuery] long? hubId, [FromQuery] DateTime? pickupDatetime, [FromQuery] DateTime? returnDatetime) =>
        Ok(await _vehicleService.GetCarTypesForHubAsync(hubId, pickupDatetime, returnDatetime));

    [HttpGet("cars/available")]
    public async Task<IActionResult> GetAvailableCars(
        [FromQuery] long? hubId, [FromQuery] DateTime? pickupDatetime, [FromQuery] DateTime? returnDatetime) =>
        Ok(await _vehicleService.GetAvailableCarsAsync(hubId, pickupDatetime, returnDatetime));

    [HttpGet("cars/{carId:long}")]
    public async Task<IActionResult> GetCarById(long carId) => Ok(await _vehicleService.GetCarByIdAsync(carId));
}
