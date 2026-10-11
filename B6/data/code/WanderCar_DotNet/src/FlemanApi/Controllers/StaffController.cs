using FlemanApi.DTO;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

[ApiController]
[Route("api/staff")]
[Authorize(Roles = "STAFF")]
public class StaffController : ControllerBase
{
    private readonly IStaffService _staffService;

    public StaffController(IStaffService staffService)
    {
        _staffService = staffService;
    }

    // The hand-over screen's car picker: every car of this booking's
    // reserved category, at its pickup hub, that's physically free right now.
    [HttpGet("bookings/{confirmationNo}/available-cars")]
    public async Task<IActionResult> GetAvailableCarsForHandover(string confirmationNo) =>
        Ok(await _staffService.GetAvailableCarsForHandoverAsync(confirmationNo));

    [HttpPost("bookings/{confirmationNo}/handover")]
    public async Task<IActionResult> HandoverVehicle(string confirmationNo, [FromBody] HandoverRequest? request) =>
        Ok(await _staffService.HandoverVehicleAsync(confirmationNo, request ?? new HandoverRequest()));

    [HttpPost("bookings/{confirmationNo}/return")]
    public async Task<IActionResult> ProcessReturn(string confirmationNo, [FromBody] ProcessReturnRequest? request) =>
        Ok(await _staffService.ProcessReturnAsync(confirmationNo, request ?? new ProcessReturnRequest()));

    // Every booking, with customer info and car already attached.
    [HttpGet("bookings")]
    public async Task<IActionResult> GetAllBookings() => Ok(await _staffService.GetAllBookingsAsync());

    // Per car type, how many are available / handed over / in maintenance
    // right now. hubId is optional — omit for a fleet-wide view.
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] long? hubId) => Ok(await _staffService.GetDashboardAsync(hubId));
}
