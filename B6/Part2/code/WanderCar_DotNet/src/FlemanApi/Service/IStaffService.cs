using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IStaffService
{
    Task<BookingResponseDTO> HandoverVehicleAsync(string confirmationNo, HandoverRequest request);
    Task<InvoiceResponseDTO> ProcessReturnAsync(string confirmationNo, ProcessReturnRequest request);
    Task<IReadOnlyList<CarDTO>> GetAvailableCarsForHandoverAsync(string confirmationNo);
    Task<IReadOnlyList<BookingResponseDTO>> GetAllBookingsAsync();
    Task<IReadOnlyList<CarTypeAvailabilityDTO>> GetDashboardAsync(long? hubId);
}
