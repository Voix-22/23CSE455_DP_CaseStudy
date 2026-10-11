using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IAirportService
{
    Task<IReadOnlyList<AirportDTO>> GetAllAirportsAsync();
    Task<IReadOnlyList<AirportDTO>> SearchAirportsAsync(string? query);
    Task<AirportDTO> GetAirportByIdAsync(long airportId);
    Task<AirportDTO> CreateAirportAsync(AirportRequest request);
    Task<AirportDTO> UpdateAirportAsync(long airportId, AirportRequest request);
    Task DeleteAirportAsync(long airportId);
}
