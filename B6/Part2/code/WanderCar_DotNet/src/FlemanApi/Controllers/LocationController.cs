using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// Public lookup APIs — mirrors com.fleman.controller.LocationController,
// permitAll for "/api/states/**", "/api/cities/**", "/api/hubs/**" per
// SecurityConfig.
[ApiController]
[Route("api")]
[AllowAnonymous]
public class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;

    public LocationController(ILocationService locationService)
    {
        _locationService = locationService;
    }

    [HttpGet("states")]
    public async Task<IActionResult> GetStates() => Ok(await _locationService.GetStatesAsync());

    [HttpGet("states/{stateId:long}")]
    public async Task<IActionResult> GetStateById(long stateId) => Ok(await _locationService.GetStateByIdAsync(stateId));

    [HttpGet("cities")]
    public async Task<IActionResult> GetCitiesByState([FromQuery] long? stateId) =>
        Ok(await _locationService.GetCitiesByStateAsync(stateId));

    [HttpGet("cities/{cityId:long}")]
    public async Task<IActionResult> GetCityById(long cityId) => Ok(await _locationService.GetCityByIdAsync(cityId));

    [HttpGet("hubs")]
    public async Task<IActionResult> GetAllHubs() => Ok(await _locationService.GetAllHubsAsync());

    [HttpGet("hubs/{hubId:long}")]
    public async Task<IActionResult> GetHubById(long hubId) => Ok(await _locationService.GetHubByIdAsync(hubId));

    [HttpGet("hubs/by-city/{cityId:long}")]
    public async Task<IActionResult> FindHubForCity(long cityId) => Ok(await _locationService.FindHubForCityAsync(cityId));

    [HttpGet("hubs/by-airport/{airportId:long}")]
    public async Task<IActionResult> FindHubForAirport(long airportId) => Ok(await _locationService.FindHubForAirportAsync(airportId));
}
