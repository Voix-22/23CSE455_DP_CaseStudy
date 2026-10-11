using FlemanApi.DTO;
using FlemanApi.Security;
using FlemanApi.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FlemanApi.Controllers;

[ApiController]
[Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IAuthenticatedUserAccessor _currentUser;
    private readonly ILogger<BookingController> _logger;

    public BookingController(IBookingService bookingService, IAuthenticatedUserAccessor currentUser, ILogger<BookingController> logger)
    {
        _bookingService = bookingService;
        _currentUser = currentUser;
        _logger = logger;
    }

    // Guest booking is permitted — SecurityConfig allows POST /api/bookings anonymously.
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        _logger.LogDebug("createBooking request: {@Request}", request);
        var response = await _bookingService.CreateBookingAsync(request);
        _logger.LogInformation("Booking created, confirmationNo={ConfirmationNo}", response.ConfirmationNo);
        return Ok(response);
    }

    [HttpGet("confirmation/{confirmationNo}")]
    public async Task<IActionResult> GetByConfirmation(string confirmationNo) =>
        Ok(await _bookingService.GetBookingByConfirmationAsync(confirmationNo));

    [HttpGet("me")]
    public async Task<IActionResult> GetLastForCustomer() =>
        Ok(await _bookingService.GetLastBookingForCustomerAsync(_currentUser.GetCurrentUserId()));

    // Every one of the customer's bookings, not just the last one — backs
    // the "modify booking" page's picker.
    [HttpGet]
    public async Task<IActionResult> GetMyBookings() =>
        Ok(await _bookingService.GetBookingsForCustomerAsync(_currentUser.GetCurrentUserId()));

    [HttpPut("confirmation/{confirmationNo}")]
    public async Task<IActionResult> ModifyBooking(string confirmationNo, [FromBody] ModifyBookingRequest request) =>
        Ok(await _bookingService.ModifyBookingAsync(confirmationNo, request));

    [HttpDelete("confirmation/{confirmationNo}")]
    public async Task<IActionResult> CancelBooking(string confirmationNo) =>
        Ok(await _bookingService.CancelBookingAsync(confirmationNo));
}
