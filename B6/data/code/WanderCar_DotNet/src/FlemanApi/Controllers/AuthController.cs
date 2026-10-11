using FlemanApi.DTO;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly ICustomerService _customerService;

    public AuthController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request) =>
        Ok(await _customerService.LoginAsync(request));

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request) =>
        Ok(await _customerService.RegisterAsync(request));

    [HttpPost("staff-login")]
    public async Task<IActionResult> StaffLogin([FromBody] StaffLoginRequest request) =>
        Ok(await _customerService.StaffLoginAsync(request));
}
