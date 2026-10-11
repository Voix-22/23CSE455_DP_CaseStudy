using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlemanApi.Controllers;

[ApiController]
[Route("profile")]
[AllowAnonymous]
public class ProfileController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public ProfileController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult GetProfile() => Ok(_configuration["MyMessage"] ?? "Development Environment");
}
