using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Service;
using FlemanApi.Service.Categories;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class CarTypeServiceTests
{
    private List<CarType> _carTypes = null!;
    private CarTypeService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _carTypes = new List<CarType>
        {
            new() { CarTypeId = 1, CarTypeName = "Sedan", DailyRate = 30.0 },
            new() { CarTypeId = 2, CarTypeName = "City EV", DailyRate = 60.0, Category = VehicleCategory.ELECTRIC, SecurityDeposit = 120.0 },
        };
        var repo = MockRepositoryFactory.Create<CarType, long>(_carTypes, ct => ct.CarTypeId, (ct, id) => ct.CarTypeId = id);
        _service = new CarTypeService(repo.Object, new VehicleCategoryRulesFactory(), TestMapperFactory.Create());
    }

    [Test]
    public async Task CreateCarTypeAsync_Electric_StoresCategoryAndDeposit()
    {
        var dto = await _service.CreateCarTypeAsync(new CreateCarTypeRequest
        {
            CarTypeName = "Premium EV", Category = VehicleCategory.ELECTRIC, DailyRate = 90.0,
        });

        dto.Category.Should().Be("ELECTRIC");
        dto.SecurityDeposit.Should().Be(180.0);
        dto.CarTypeId.Should().Be(3);
        _carTypes.Should().HaveCount(3);
    }

    [Test]
    public async Task CreateCarTypeAsync_Commercial_WithWeeklyRate_Succeeds()
    {
        var dto = await _service.CreateCarTypeAsync(new CreateCarTypeRequest
        {
            CarTypeName = "Cargo Van", Category = VehicleCategory.COMMERCIAL, DailyRate = 80.0, WeeklyRate = 500.0,
        });

        dto.Category.Should().Be("COMMERCIAL");
        dto.SecurityDeposit.Should().Be(400.0);
    }

    [Test]
    public async Task CreateCarTypeAsync_Commercial_WithoutWeeklyRate_ThrowsBadRequest()
    {
        var act = () => _service.CreateCarTypeAsync(new CreateCarTypeRequest
        {
            CarTypeName = "Cargo Van", Category = VehicleCategory.COMMERCIAL, DailyRate = 80.0,
        });

        (await act.Should().ThrowAsync<ApiException>()).Which.Status.Should().Be(HttpStatusCode.BadRequest);
        _carTypes.Should().HaveCount(2);
    }

    [Test]
    public async Task CreateCarTypeAsync_DuplicateName_IgnoringCaseAndSpaces_ThrowsConflict()
    {
        var act = () => _service.CreateCarTypeAsync(new CreateCarTypeRequest
        {
            CarTypeName = "  sedan ", Category = VehicleCategory.STANDARD, DailyRate = 30.0,
        });

        (await act.Should().ThrowAsync<ApiException>()).Which.Status.Should().Be(HttpStatusCode.Conflict);
        _carTypes.Should().HaveCount(2);
    }

    [Test]
    public async Task CreateCarTypeAsync_Standard_HasZeroDeposit()
    {
        var dto = await _service.CreateCarTypeAsync(new CreateCarTypeRequest
        {
            CarTypeName = "Hatchback", Category = VehicleCategory.STANDARD, DailyRate = 18.0,
        });

        dto.Category.Should().Be("STANDARD");
        dto.SecurityDeposit.Should().Be(0.0);
    }

    [Test]
    public async Task GetCarTypesByCategoryAsync_ReturnsOnlyThatCategory()
    {
        var electric = await _service.GetCarTypesByCategoryAsync(VehicleCategory.ELECTRIC);
        var standard = await _service.GetCarTypesByCategoryAsync(VehicleCategory.STANDARD);
        var commercial = await _service.GetCarTypesByCategoryAsync(VehicleCategory.COMMERCIAL);

        electric.Should().ContainSingle().Which.CarTypeName.Should().Be("City EV");
        standard.Should().ContainSingle().Which.CarTypeName.Should().Be("Sedan");
        commercial.Should().BeEmpty();
    }

    [Test]
    public void Mapping_CarTypeWithoutExplicitCategory_IsReportedAsStandard()
    {
        var dto = TestMapperFactory.Create().Map<CarTypeDTO>(new CarType { CarTypeId = 9, CarTypeName = "SUV", DailyRate = 40.0 });

        dto.Category.Should().Be("STANDARD");
        dto.SecurityDeposit.Should().Be(0.0);
    }
}
