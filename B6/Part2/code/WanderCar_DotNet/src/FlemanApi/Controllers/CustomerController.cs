using FlemanApi.DTO;
using FlemanApi.Security;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly IAuthenticatedUserAccessor _currentUser;

    public CustomerController(ICustomerService customerService, IAuthenticatedUserAccessor currentUser)
    {
        _customerService = customerService;
        _currentUser = currentUser;
    }

    [HttpGet("{customerId:long}")]
    public async Task<IActionResult> GetCustomerById(long customerId) =>
        Ok(await _customerService.GetCustomerByIdAsync(customerId));

    // Separate from the main customer read — see GovtIdDocumentDTO for why.
    [HttpGet("{customerId:long}/govt-id")]
    public async Task<IActionResult> GetGovtIdDocument(long customerId) =>
        Ok(await _customerService.GetGovtIdDocumentAsync(customerId));

    [HttpPut]
    public async Task<IActionResult> UpdateCustomer([FromBody] UpdateCustomerRequest request) =>
        Ok(await _customerService.UpdateCustomerAsync(request));

    [HttpGet("me")]
    public async Task<IActionResult> Me() =>
        Ok(await _customerService.GetCustomerByIdAsync(_currentUser.GetCurrentUserId()));

    // Used by the booking flow for a guest who fills in their details
    // without logging in first.
    [HttpPost("guest")]
    [AllowAnonymous]
    public async Task<IActionResult> UpsertGuestCustomer([FromBody] GuestCustomerRequest request) =>
        Ok(await _customerService.UpsertGuestCustomerAsync(request));
}
