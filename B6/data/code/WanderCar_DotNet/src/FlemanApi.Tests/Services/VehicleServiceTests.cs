using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class VehicleServiceTests
{
    private List<Car> _cars = null!;
    private List<CarType> _carTypes = null!;
    private Mock<IAvailabilityRepository> _availabilityMock = null!;
    private VehicleService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _cars = new List<Car>
        {
            new() { CarId = 1, CarTypeId = 1, HubId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE },
            new() { CarId = 2, CarTypeId = 1, HubId = 2, VehicleNumber = "MH01AA2222", Status = CarStatus.BOOKED },
        };
        _carTypes = new List<CarType>
        {
            new() { CarTypeId = 1, CarTypeName = "Sedan", DailyRate = 100.0 },
            new() { CarTypeId = 2, CarTypeName = "SUV", DailyRate = 150.0 },
        };

        var carsRepo = MockRepositoryFactory.Create<Car, long>(_cars, c => c.CarId);
        var carTypesRepo = MockRepositoryFactory.Create<CarType, long>(_carTypes, ct => ct.CarTypeId);
        _availabilityMock = new Mock<IAvailabilityRepository>();

        _service = new VehicleService(carsRepo.Object, carTypesRepo.Object, _availabilityMock.Object, TestMapperFactory.Create());
    }

    [Test]
    public async Task GetCarTypesAsync_ReturnsAllCarTypes()
    {
        var result = await _service.GetCarTypesAsync();

        result.Should().HaveCount(2);
    }

    [Test]
    public async Task GetCarTypesForHubAsync_NullHubId_ReturnsAllCarTypesWithoutAvailability()
    {
        var result = await _service.GetCarTypesForHubAsync(null, null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(ct => ct.AvailableCount == null);
    }

    [Test]
    public async Task GetCarTypesForHubAsync_CapacityMinusOverlap_ComputesAvailableCount()
    {
        _availabilityMock.Setup(a => a.CountCapacityAsync(1, It.IsAny<long>())).ReturnsAsync(5);
        _availabilityMock.Setup(a => a.CountOverlappingAsync(1, It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(2);

        var result = await _service.GetCarTypesForHubAsync(1, DateTime.Now, DateTime.Now.AddDays(3));

        result.Should().OnlyContain(ct => ct.AvailableCount == 3);
    }

    [Test]
    public async Task GetCarTypesForHubAsync_OverlapExceedsCapacity_ClampsToZero()
    {
        _availabilityMock.Setup(a => a.CountCapacityAsync(1, It.IsAny<long>())).ReturnsAsync(1);
        _availabilityMock.Setup(a => a.CountOverlappingAsync(1, It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(5);

        var result = await _service.GetCarTypesForHubAsync(1, DateTime.Now, DateTime.Now.AddDays(3));

        result.Should().OnlyContain(ct => ct.AvailableCount == 0);
    }

    [Test]
    public async Task GetCarTypesForHubAsync_NoDatesGiven_OverlapIsZero()
    {
        _availabilityMock.Setup(a => a.CountCapacityAsync(1, It.IsAny<long>())).ReturnsAsync(4);

        var result = await _service.GetCarTypesForHubAsync(1, null, null);

        result.Should().OnlyContain(ct => ct.AvailableCount == 4);
    }

    [Test]
    public async Task GetAvailableCarsAsync_NullHubId_ReturnsAllCarsBookableTrue()
    {
        var result = await _service.GetAvailableCarsAsync(null, null, null);

        result.Should().HaveCount(2);
        result.Should().OnlyContain(c => c.BookableNow);
    }

    [Test]
    public async Task GetAvailableCarsAsync_WithHubId_BookableNowReflectsCarStatus()
    {
        var result = await _service.GetAvailableCarsAsync(1, null, null);

        result.Should().ContainSingle();
        result[0].BookableNow.Should().BeTrue();
    }

    [Test]
    public async Task GetAvailableCarsAsync_BookedCarAtHub_BookableNowFalse()
    {
        var result = await _service.GetAvailableCarsAsync(2, null, null);

        result.Should().ContainSingle();
        result[0].BookableNow.Should().BeFalse();
    }

    [Test]
    public async Task GetCarByIdAsync_ExistingCar_ReturnsCarWithNestedCarType()
    {
        var result = await _service.GetCarByIdAsync(1);

        result.CarId.Should().Be(1);
        result.CarType.Should().NotBeNull();
        result.CarType!.CarTypeName.Should().Be("Sedan");
    }

    [Test]
    public async Task GetCarByIdAsync_MissingCar_ThrowsResourceNotFound()
    {
        var act = async () => await _service.GetCarByIdAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task GetCarTypeByIdAsync_MissingCarType_ThrowsResourceNotFound()
    {
        var act = async () => await _service.GetCarTypeByIdAsync(999);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }
}
