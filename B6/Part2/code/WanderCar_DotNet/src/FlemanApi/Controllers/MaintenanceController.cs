using FlemanApi.DTO;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// CH03 - fleet-maintenance scheduling, staff only.
[ApiController]
[Route("api/staff/maintenance")]
[Authorize(Roles = "STAFF")]
public class MaintenanceController : ControllerBase
{
    private readonly IMaintenanceService _maintenanceService;

    public MaintenanceController(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    [HttpPost]
    public async Task<IActionResult> Schedule([FromBody] ScheduleMaintenanceRequest request) =>
        Ok(await _maintenanceService.ScheduleAsync(request));

    // SCHEDULED items dated today.
    [HttpGet("due")]
    public async Task<IActionResult> GetDue() => Ok(await _maintenanceService.GetDueAsync());

    // SCHEDULED items dated before today.
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue() => Ok(await _maintenanceService.GetOverdueAsync());

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id) => Ok(await _maintenanceService.GetByIdAsync(id));

    [HttpPost("{id:long}/start")]
    public async Task<IActionResult> Start(long id, [FromBody] MaintenanceTransitionRequest? request) =>
        Ok(await _maintenanceService.StartAsync(id, request ?? new MaintenanceTransitionRequest()));

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, [FromBody] MaintenanceTransitionRequest? request) =>
        Ok(await _maintenanceService.CompleteAsync(id, request ?? new MaintenanceTransitionRequest()));

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, [FromBody] MaintenanceTransitionRequest? request) =>
        Ok(await _maintenanceService.CancelAsync(id, request ?? new MaintenanceTransitionRequest()));
}
