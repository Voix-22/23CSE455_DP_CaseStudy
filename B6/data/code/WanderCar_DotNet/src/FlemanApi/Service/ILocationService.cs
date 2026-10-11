using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface ILocationService
{
    Task<IReadOnlyList<StateDTO>> GetStatesAsync();
    Task<StateDTO> GetStateByIdAsync(long stateId);
    Task<IReadOnlyList<CityDTO>> GetCitiesByStateAsync(long? stateId);
    Task<CityDTO> GetCityByIdAsync(long cityId);
    Task<IReadOnlyList<HubDTO>> GetAllHubsAsync();
    Task<HubDTO> GetHubByIdAsync(long hubId);
    Task<HubDTO?> FindHubForCityAsync(long cityId);
    Task<HubDTO?> FindHubForAirportAsync(long airportId);
}
