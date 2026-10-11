using FlemanApi.DTO;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// The complete Airport Master API — mirrors com.fleman.controller.AirportController.
// "/api/airports/**" is permitAll per SecurityConfig, including the writes
// (not staff-restricted in the Java app either — replicated as-is).
[ApiController]
[Route("api/airports")]
[AllowAnonymous]
public class AirportController : ControllerBase
{
    private readonly IAirportService _airportService;

    public AirportController(IAirportService airportService)
    {
        _airportService = airportService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAirports() => Ok(await _airportService.GetAllAirportsAsync());

    [HttpGet("search")]
    public async Task<IActionResult> SearchAirports([FromQuery] string? q) => Ok(await _airportService.SearchAirportsAsync(q));

    [HttpGet("{airportId:long}")]
    public async Task<IActionResult> GetAirportById(long airportId) => Ok(await _airportService.GetAirportByIdAsync(airportId));

    [HttpPost]
    public async Task<IActionResult> CreateAirport([FromBody] AirportRequest request) =>
        Ok(await _airportService.CreateAirportAsync(request));

    [HttpPut("{airportId:long}")]
    public async Task<IActionResult> UpdateAirport(long airportId, [FromBody] AirportRequest request) =>
        Ok(await _airportService.UpdateAirportAsync(airportId, request));

    [HttpDelete("{airportId:long}")]
    public async Task<IActionResult> DeleteAirport(long airportId)
    {
        await _airportService.DeleteAirportAsync(airportId);
        return NoContent();
    }
}
