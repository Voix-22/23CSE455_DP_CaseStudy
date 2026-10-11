using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

// Debug/test endpoint — mirrors com.fleman.controller.EmailController.
// Not in SecurityConfig's permitAll list, so it requires authentication
// (no specific role), same as the Java app's default anyRequest().authenticated().
[ApiController]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailService _emailService;

    public EmailController(IEmailService emailService)
    {
        _emailService = emailService;
    }

    [HttpGet("api/email/test")]
    public async Task<IActionResult> SendTestEmail()
    {
        await _emailService.SendEmailAsync(
            "teamgryffindor45@gmail.com",
            "Test Email from WanderCar",
            "Congratulations! Your Email Service is working successfully.");

        return Ok("Email sent successfully!");
    }
}
