using AutoMapper;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.LocationServiceImpl.
public class LocationService : ILocationService
{
    private readonly IGenericRepository<State, long> _states;
    private readonly IGenericRepository<City, long> _cities;
    private readonly IGenericRepository<Hub, long> _hubs;
    private readonly IGenericRepository<Airport, long> _airports;
    private readonly IMapper _mapper;

    public LocationService(
        IGenericRepository<State, long> states,
        IGenericRepository<City, long> cities,
        IGenericRepository<Hub, long> hubs,
        IGenericRepository<Airport, long> airports,
        IMapper mapper)
    {
        _states = states;
        _cities = cities;
        _hubs = hubs;
        _airports = airports;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<StateDTO>> GetStatesAsync() =>
        _mapper.Map<List<StateDTO>>(await _states.GetAllAsync());

    public async Task<StateDTO> GetStateByIdAsync(long stateId)
    {
        var state = await _states.GetByIdAsync(stateId)
            ?? throw new ResourceNotFoundException($"State not found: {stateId}");
        return _mapper.Map<StateDTO>(state);
    }

    public async Task<IReadOnlyList<CityDTO>> GetCitiesByStateAsync(long? stateId)
    {
        var cities = stateId is null
            ? await _cities.GetAllAsync()
            : await _cities.FindAsync(c => c.StateId == stateId.Value);
        return _mapper.Map<List<CityDTO>>(cities);
    }

    public async Task<CityDTO> GetCityByIdAsync(long cityId)
    {
        var city = await _cities.GetByIdAsync(cityId)
            ?? throw new ResourceNotFoundException($"City not found: {cityId}");
        return _mapper.Map<CityDTO>(city);
    }

    public async Task<IReadOnlyList<HubDTO>> GetAllHubsAsync() =>
        _mapper.Map<List<HubDTO>>(await _hubs.GetAllAsync());

    public async Task<HubDTO> GetHubByIdAsync(long hubId)
    {
        var hub = await _hubs.GetByIdAsync(hubId)
            ?? throw new ResourceNotFoundException($"Hub not found: {hubId}");
        return _mapper.Map<HubDTO>(hub);
    }

    public async Task<HubDTO?> FindHubForCityAsync(long cityId)
    {
        var hub = await _hubs.FirstOrDefaultAsync(h => h.CityId == cityId);
        return hub is null ? null : _mapper.Map<HubDTO>(hub);
    }

    public async Task<HubDTO?> FindHubForAirportAsync(long airportId)
    {
        var airport = await _airports.GetByIdAsync(airportId);
        if (airport is null) return null;

        // No relationship traversal — a second, explicit lookup by the
        // plain HubId column, same as any other logical reference.
        var hub = await _hubs.GetByIdAsync(airport.HubId);
        return hub is null ? null : _mapper.Map<HubDTO>(hub);
    }
}
