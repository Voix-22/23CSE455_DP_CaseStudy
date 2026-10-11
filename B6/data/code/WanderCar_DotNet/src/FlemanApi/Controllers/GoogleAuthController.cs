using System.Security.Claims;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace FlemanApi.Controllers;

// Mirrors CustomOAuth2UserService (find-or-create the Customer, stamp
// provider/providerId/emailVerified) + OAuth2SuccessHandler (issue a JWT,
// redirect the browser back to the frontend with it) combined into one
// callback, since ASP.NET Core's Google handler doesn't have a direct
// equivalent to Spring Security's two-hook OAuth2 pipeline.
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class GoogleAuthController : ControllerBase
{
    private readonly IGenericRepository<Customer, long> _customers;
    private readonly ICustomerService _customerService;
    private readonly IConfiguration _configuration;

    public GoogleAuthController(
        IGenericRepository<Customer, long> customers, ICustomerService customerService, IConfiguration configuration)
    {
        _customers = customers;
        _customerService = customerService;
        _configuration = configuration;
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        if (!IsGoogleOAuthConfigured())
        {
            return Problem("Google OAuth is not configured on this server — set GoogleOAuth:ClientId/ClientSecret.", statusCode: 503);
        }

        var redirectUrl = Url.Action(nameof(GoogleCallback), "GoogleAuth")!;
        var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        if (!IsGoogleOAuthConfigured())
        {
            return Problem("Google OAuth is not configured on this server — set GoogleOAuth:ClientId/ClientSecret.", statusCode: 503);
        }

        var authenticateResult = await HttpContext.AuthenticateAsync("GoogleSignIn");
        if (!authenticateResult.Succeeded || authenticateResult.Principal is null)
        {
            return Unauthorized();
        }

        var principal = authenticateResult.Principal;
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var name = principal.FindFirstValue(ClaimTypes.Name);
        var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var emailVerified = bool.TryParse(principal.FindFirstValue("email_verified"), out var v) && v;

        await HttpContext.SignOutAsync("GoogleSignIn");

        if (string.IsNullOrEmpty(email))
        {
            return BadRequest("Google did not return an email address");
        }

        var customer = await _customers.FirstOrDefaultAsync(c => c.Email.ToLower() == email.ToLower());
        if (customer is null)
        {
            customer = new Customer { Email = email, FullName = name ?? email, Status = CustomerStatus.ACTIVE };
            await _customers.AddAsync(customer);
        }

        customer.Provider = AuthProvider.GOOGLE;
        customer.ProviderId = googleId;
        customer.EmailVerified = emailVerified;
        await _customers.SaveChangesAsync();

        var token = _customerService.IssueCustomerToken(customer);
        var frontendBaseUrl = _configuration["FrontendBaseUrl"] ?? "http://localhost:5173";
        return Redirect($"{frontendBaseUrl}/oauth-success?token={token}");
    }

    private bool IsGoogleOAuthConfigured()
    {
        var section = _configuration.GetSection("GoogleOAuth");
        return !string.IsNullOrWhiteSpace(section["ClientId"]) && !string.IsNullOrWhiteSpace(section["ClientSecret"]);
    }
}
