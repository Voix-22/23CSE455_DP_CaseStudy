using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// Requirement #11 — proof-of-pattern that this .NET backend can call the
// legacy Java Spring Boot service (fleman-backend) via HttpClient. Requires
// fleman-backend to be running on JavaMicroservice:BaseUrl (default
// http://localhost:8080) — see the README for running both side by side.
[ApiController]
[Route("api/legacy")]
[AllowAnonymous]
public class LegacyProxyController : ControllerBase
{
    private readonly IJavaMicroserviceClient _javaClient;

    public LegacyProxyController(IJavaMicroserviceClient javaClient)
    {
        _javaClient = javaClient;
    }

    [HttpGet("states")]
    public async Task<IActionResult> ForwardStates()
    {
        var json = await _javaClient.GetStatesAsync();
        return Content(json, "application/json");
    }

    [HttpGet("health")]
    public async Task<IActionResult> ForwardHealth()
    {
        var text = await _javaClient.GetHealthAsync();
        return Content(text, "text/plain");
    }
}
