using System.Net;
using AutoMapper;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.AirportServiceImpl — the Airport Master
// API's create/update/delete enforce uniqueness and cross-reference
// existence manually, since there's no DB-level FK/unique constraint doing
// it for us (this project's entities are all plain scalar-FK columns).
public class AirportService : IAirportService
{
    private readonly IGenericRepository<Airport, long> _airports;
    private readonly IGenericRepository<City, long> _cities;
    private readonly IGenericRepository<State, long> _states;
    private readonly IGenericRepository<Hub, long> _hubs;
    private readonly IMapper _mapper;

    public AirportService(
        IGenericRepository<Airport, long> airports,
        IGenericRepository<City, long> cities,
        IGenericRepository<State, long> states,
        IGenericRepository<Hub, long> hubs,
        IMapper mapper)
    {
        _airports = airports;
        _cities = cities;
        _states = states;
        _hubs = hubs;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<AirportDTO>> GetAllAirportsAsync() =>
        _mapper.Map<List<AirportDTO>>(await _airports.GetAllAsync());

    public async Task<IReadOnlyList<AirportDTO>> SearchAirportsAsync(string? query)
    {
        var q = query?.Trim() ?? string.Empty;
        IReadOnlyList<Airport> results;
        if (q.Length == 0)
        {
            results = (await _airports.GetAllAsync()).Take(6).ToList();
        }
        else
        {
            var lower = q.ToLower();
            results = await _airports.FindAsync(a =>
                a.AirportCode.ToLower().Contains(lower) || a.AirportName.ToLower().Contains(lower));
        }
        return _mapper.Map<List<AirportDTO>>(results);
    }

    public async Task<AirportDTO> GetAirportByIdAsync(long airportId)
    {
        var airport = await _airports.GetByIdAsync(airportId)
            ?? throw new ResourceNotFoundException($"Airport not found: {airportId}");
        return _mapper.Map<AirportDTO>(airport);
    }

    public async Task<AirportDTO> CreateAirportAsync(AirportRequest request)
    {
        if (await _airports.ExistsAsync(a => a.AirportCode.ToLower() == request.AirportCode.ToLower()))
        {
            throw new ApiException($"An airport with this code already exists: {request.AirportCode}", HttpStatusCode.Conflict);
        }

        var airport = new Airport { AirportName = request.AirportName, AirportCode = request.AirportCode };
        await ApplyReferencesAsync(airport, request);

        await _airports.AddAsync(airport);
        await _airports.SaveChangesAsync();
        return _mapper.Map<AirportDTO>(airport);
    }

    public async Task<AirportDTO> UpdateAirportAsync(long airportId, AirportRequest request)
    {
        var airport = await _airports.GetByIdAsync(airportId)
            ?? throw new ResourceNotFoundException($"Airport not found: {airportId}");

        if (await _airports.ExistsAsync(a =>
                a.AirportCode.ToLower() == request.AirportCode.ToLower() && a.AirportId != airportId))
        {
            throw new ApiException($"An airport with this code already exists: {request.AirportCode}", HttpStatusCode.Conflict);
        }

        airport.AirportName = request.AirportName;
        airport.AirportCode = request.AirportCode;
        await ApplyReferencesAsync(airport, request);

        await _airports.SaveChangesAsync();
        return _mapper.Map<AirportDTO>(airport);
    }

    public async Task DeleteAirportAsync(long airportId)
    {
        var airport = await _airports.GetByIdAsync(airportId)
            ?? throw new ResourceNotFoundException($"Airport not found: {airportId}");
        _airports.Remove(airport);
        await _airports.SaveChangesAsync();
    }

    // No relationships to attach — just app-level referential-integrity
    // checks (each id must resolve to a real row) before the plain id
    // columns are set, since there's no database-level FK constraint.
    private async Task ApplyReferencesAsync(Airport airport, AirportRequest request)
    {
        if (!await _cities.ExistsAsync(c => c.CityId == request.CityId))
            throw new ResourceNotFoundException($"City not found: {request.CityId}");
        if (!await _states.ExistsAsync(s => s.StateId == request.StateId))
            throw new ResourceNotFoundException($"State not found: {request.StateId}");
        if (!await _hubs.ExistsAsync(h => h.HubId == request.HubId))
            throw new ResourceNotFoundException($"Hub not found: {request.HubId}");

        airport.CityId = request.CityId!.Value;
        airport.StateId = request.StateId!.Value;
        airport.HubId = request.HubId!.Value;
    }
}
