using FlemanApi.DTO;
using FlemanApi.Models;

namespace FlemanApi.Service;

// CH02 - car-type administration by category. Kept apart from
// IVehicleService so the existing read-only catalogue is untouched.
public interface ICarTypeService
{
    Task<CarTypeDTO> CreateCarTypeAsync(CreateCarTypeRequest request);
    Task<IReadOnlyList<CarTypeDTO>> GetCarTypesByCategoryAsync(VehicleCategory category);
}
