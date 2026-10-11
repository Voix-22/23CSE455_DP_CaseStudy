using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IBookingService
{
    Task<BookingResponseDTO> CreateBookingAsync(CreateBookingRequest request);
    Task<BookingResponseDTO> GetBookingByConfirmationAsync(string confirmationNo);
    Task<BookingResponseDTO?> GetLastBookingForCustomerAsync(long customerId);
    Task<IReadOnlyList<BookingResponseDTO>> GetBookingsForCustomerAsync(long customerId);
    Task<BookingResponseDTO> ModifyBookingAsync(string confirmationNo, ModifyBookingRequest request);
    Task<BookingResponseDTO> CancelBookingAsync(string confirmationNo);
}
