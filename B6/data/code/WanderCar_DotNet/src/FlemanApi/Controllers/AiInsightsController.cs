using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// Requirement #3 — Microsoft.Extensions.AI-backed staff dashboard summary.
[ApiController]
[Route("api/staff/dashboard")]
[Authorize(Roles = "STAFF")]
public class AiInsightsController : ControllerBase
{
    private readonly IStaffService _staffService;
    private readonly IAiInsightsService _aiInsightsService;

    public AiInsightsController(IStaffService staffService, IAiInsightsService aiInsightsService)
    {
        _staffService = staffService;
        _aiInsightsService = aiInsightsService;
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetFleetSummary([FromQuery] long? hubId)
    {
        var stats = await _staffService.GetDashboardAsync(hubId);
        var summary = await _aiInsightsService.GenerateFleetSummaryAsync(stats);
        return Ok(new { summary, stats });
    }
}
