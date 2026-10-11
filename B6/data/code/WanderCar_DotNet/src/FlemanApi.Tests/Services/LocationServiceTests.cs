using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class LocationServiceTests
{
    private List<State> _states = null!;
    private List<City> _cities = null!;
    private List<Hub> _hubs = null!;
    private List<Airport> _airports = null!;
    private LocationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _states = new List<State> { new() { StateId = 1, StateName = "Maharashtra" } };
        _cities = new List<City> { new() { CityId = 1, CityName = "Mumbai", StateId = 1 } };
        _hubs = new List<Hub>
        {
            new() { HubId = 1, HubName = "Mumbai Hub", CityId = 1, StateId = 1, Pincode = "400001", ContactNo = "1234567890" },
        };
        _airports = new List<Airport> { new() { AirportId = 1, AirportCode = "BOM", AirportName = "Mumbai Intl", CityId = 1, StateId = 1, HubId = 1 } };

        var statesRepo = MockRepositoryFactory.Create<State, long>(_states, s => s.StateId);
        var citiesRepo = MockRepositoryFactory.Create<City, long>(_cities, c => c.CityId);
        var hubsRepo = MockRepositoryFactory.Create<Hub, long>(_hubs, h => h.HubId);
        var airportsRepo = MockRepositoryFactory.Create<Airport, long>(_airports, a => a.AirportId);

        _service = new LocationService(statesRepo.Object, citiesRepo.Object, hubsRepo.Object, airportsRepo.Object, TestMapperFactory.Create());
    }

    [Test]
    public async Task GetStatesAsync_ReturnsAllStates()
    {
        var result = await _service.GetStatesAsync();

        result.Should().ContainSingle(s => s.StateName == "Maharashtra");
    }

    [Test]
    public async Task GetStateByIdAsync_ExistingId_ReturnsState()
    {
        var result = await _service.GetStateByIdAsync(1);

        result.StateName.Should().Be("Maharashtra");
    }

    [Test]
    public async Task GetStateByIdAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.GetStateByIdAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task GetCitiesByStateAsync_WithStateId_FiltersToThatState()
    {
        _cities.Add(new City { CityId = 2, CityName = "Bengaluru", StateId = 2 });

        var result = await _service.GetCitiesByStateAsync(1);

        result.Should().ContainSingle(c => c.CityName == "Mumbai");
    }

    [Test]
    public async Task GetCitiesByStateAsync_NullStateId_ReturnsAllCities()
    {
        _cities.Add(new City { CityId = 2, CityName = "Bengaluru", StateId = 2 });

        var result = await _service.GetCitiesByStateAsync(null);

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetCityByIdAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.GetCityByIdAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task GetAllHubsAsync_ReturnsAllHubs()
    {
        var result = await _service.GetAllHubsAsync();

        result.Should().ContainSingle(h => h.HubName == "Mumbai Hub");
    }

    [Test]
    public async Task GetHubByIdAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.GetHubByIdAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task FindHubForCityAsync_ExistingCity_ReturnsHub()
    {
        var result = await _service.FindHubForCityAsync(1);

        result.Should().NotBeNull();
        result!.HubName.Should().Be("Mumbai Hub");
    }

    [Test]
    public async Task FindHubForCityAsync_NoHubForCity_ReturnsNull()
    {
        var result = await _service.FindHubForCityAsync(999);

        result.Should().BeNull();
    }

    [Test]
    public async Task FindHubForAirportAsync_ExistingAirport_ReturnsHub()
    {
        var result = await _service.FindHubForAirportAsync(1);

        result.Should().NotBeNull();
        result!.HubName.Should().Be("Mumbai Hub");
    }

    [Test]
    public async Task FindHubForAirportAsync_MissingAirport_ReturnsNull()
    {
        var result = await _service.FindHubForAirportAsync(999);

        result.Should().BeNull();
    }
}
