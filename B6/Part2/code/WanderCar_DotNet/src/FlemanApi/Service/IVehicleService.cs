using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IVehicleService
{
    Task<IReadOnlyList<CarTypeDTO>> GetCarTypesAsync();
    Task<IReadOnlyList<CarTypeDTO>> GetCarTypesForHubAsync(long? hubId, DateTime? pickupDatetime, DateTime? returnDatetime);
    Task<IReadOnlyList<CarDTO>> GetAvailableCarsAsync(long? hubId, DateTime? pickupDatetime, DateTime? returnDatetime);
    Task<CarDTO> GetCarByIdAsync(long carId);
    Task<CarTypeDTO> GetCarTypeByIdAsync(long carTypeId);
}
