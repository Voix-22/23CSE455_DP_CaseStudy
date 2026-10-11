using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class AirportServiceTests
{
    private List<Airport> _airports = null!;
    private List<City> _cities = null!;
    private List<State> _states = null!;
    private List<Hub> _hubs = null!;
    private AirportService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _airports = new List<Airport>
        {
            new() { AirportId = 1, AirportCode = "BOM", AirportName = "Mumbai Intl", CityId = 1, StateId = 1, HubId = 1 },
        };
        _cities = new List<City> { new() { CityId = 1, CityName = "Mumbai", StateId = 1 } };
        _states = new List<State> { new() { StateId = 1, StateName = "Maharashtra" } };
        _hubs = new List<Hub>
        {
            new() { HubId = 1, HubName = "Mumbai Hub", CityId = 1, StateId = 1, Pincode = "400001", ContactNo = "1234567890" },
        };

        var airportsRepo = MockRepositoryFactory.Create<Airport, long>(_airports, a => a.AirportId, (a, id) => a.AirportId = id);
        var citiesRepo = MockRepositoryFactory.Create<City, long>(_cities, c => c.CityId);
        var statesRepo = MockRepositoryFactory.Create<State, long>(_states, s => s.StateId);
        var hubsRepo = MockRepositoryFactory.Create<Hub, long>(_hubs, h => h.HubId);

        _service = new AirportService(airportsRepo.Object, citiesRepo.Object, statesRepo.Object, hubsRepo.Object, TestMapperFactory.Create());
    }

    private static AirportRequest MakeRequest(string code = "DEL") => new()
    {
        AirportName = "New Airport",
        AirportCode = code,
        CityId = 1,
        StateId = 1,
        HubId = 1,
    };

    [Test]
    public async Task GetAllAirportsAsync_ReturnsAllAirports()
    {
        var result = await _service.GetAllAirportsAsync();

        result.Should().ContainSingle(a => a.AirportCode == "BOM");
    }

    [Test]
    public async Task SearchAirportsAsync_EmptyQuery_ReturnsUpToSixAirports()
    {
        var result = await _service.SearchAirportsAsync("");

        result.Should().ContainSingle();
    }

    [Test]
    public async Task SearchAirportsAsync_MatchingCode_ReturnsMatch()
    {
        var result = await _service.SearchAirportsAsync("bom");

        result.Should().ContainSingle(a => a.AirportCode == "BOM");
    }

    [Test]
    public async Task SearchAirportsAsync_NoMatch_ReturnsEmpty()
    {
        var result = await _service.SearchAirportsAsync("xyz");

        result.Should().BeEmpty();
    }

    [Test]
    public async Task GetAirportByIdAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.GetAirportByIdAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task CreateAirportAsync_NewCode_CreatesAirport()
    {
        var created = await _service.CreateAirportAsync(MakeRequest("DEL"));

        created.AirportCode.Should().Be("DEL");
        _airports.Should().Contain(a => a.AirportCode == "DEL");
    }

    [Test]
    public async Task CreateAirportAsync_DuplicateCode_ThrowsConflict()
    {
        var act = async () => await _service.CreateAirportAsync(MakeRequest("BOM"));

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Test]
    public async Task CreateAirportAsync_DuplicateCodeDifferentCase_ThrowsConflict()
    {
        var act = async () => await _service.CreateAirportAsync(MakeRequest("bom"));

        await act.Should().ThrowAsync<ApiException>();
    }

    [Test]
    public async Task CreateAirportAsync_UnknownCity_ThrowsResourceNotFound()
    {
        var request = MakeRequest("DEL");
        request.CityId = 999;

        var act = async () => await _service.CreateAirportAsync(request);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task CreateAirportAsync_UnknownState_ThrowsResourceNotFound()
    {
        var request = MakeRequest("DEL");
        request.StateId = 999;

        var act = async () => await _service.CreateAirportAsync(request);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task CreateAirportAsync_UnknownHub_ThrowsResourceNotFound()
    {
        var request = MakeRequest("DEL");
        request.HubId = 999;

        var act = async () => await _service.CreateAirportAsync(request);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task UpdateAirportAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.UpdateAirportAsync(999, MakeRequest("DEL"));

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task UpdateAirportAsync_CodeCollidesWithAnotherAirport_ThrowsConflict()
    {
        _airports.Add(new Airport { AirportId = 2, AirportCode = "DEL", AirportName = "Delhi", CityId = 1, StateId = 1, HubId = 1 });

        var act = async () => await _service.UpdateAirportAsync(2, MakeRequest("BOM"));

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(System.Net.HttpStatusCode.Conflict);
    }

    [Test]
    public async Task UpdateAirportAsync_SameAirportSameCode_DoesNotThrow()
    {
        var updated = await _service.UpdateAirportAsync(1, MakeRequest("BOM"));

        updated.AirportCode.Should().Be("BOM");
        updated.AirportName.Should().Be("New Airport");
    }

    [Test]
    public async Task DeleteAirportAsync_ExistingId_RemovesAirport()
    {
        await _service.DeleteAirportAsync(1);

        _airports.Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAirportAsync_MissingId_ThrowsResourceNotFound()
    {
        var act = async () => await _service.DeleteAirportAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
}
