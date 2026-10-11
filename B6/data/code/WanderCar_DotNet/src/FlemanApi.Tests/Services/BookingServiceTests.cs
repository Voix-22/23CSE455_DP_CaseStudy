using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Security;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class BookingServiceTests
{
    private List<BookingHeader> _bookings = null!;
    private List<BookingDetail> _bookingDetails = null!;
    private List<CarType> _carTypes = null!;
    private List<Customer> _customers = null!;
    private List<Hub> _hubs = null!;
    private List<Addon> _addons = null!;
    private List<Car> _cars = null!;

    private Mock<IAvailabilityRepository> _availabilityMock = null!;
    private Mock<IVehicleService> _vehicleServiceMock = null!;
    private Mock<ICustomerService> _customerServiceMock = null!;
    private Mock<ILocationService> _locationServiceMock = null!;
    private Mock<IAddonService> _addonServiceMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IAuthenticatedUserAccessor> _currentUserMock = null!;

    private BookingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bookings = new List<BookingHeader>();
        _bookingDetails = new List<BookingDetail>();
        _carTypes = new List<CarType> { new() { CarTypeId = 1, CarTypeName = "Sedan", DailyRate = 100.0 } };
        _customers = new List<Customer> { new() { CustomerId = 1, FullName = "Jane Doe", Email = "jane@example.com" } };
        _hubs = new List<Hub>
        {
            new() { HubId = 1, HubName = "Pickup Hub", CityId = 1, StateId = 1, Pincode = "400001", ContactNo = "111" },
            new() { HubId = 2, HubName = "Drop Hub", CityId = 1, StateId = 1, Pincode = "400002", ContactNo = "222" },
        };
        _addons = new List<Addon> { new() { AddonId = 1, AddonName = "GPS", DailyRate = 10.0 } };
        _cars = new List<Car> { new() { CarId = 1, CarTypeId = 1, HubId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE } };

        var bookingsRepo = MockRepositoryFactory.Create<BookingHeader, long>(_bookings, b => b.BookingId, (b, id) => b.BookingId = id);
        var bookingDetailsRepo = MockRepositoryFactory.Create<BookingDetail, long>(_bookingDetails, d => d.BookingDetailId, (d, id) => d.BookingDetailId = id);
        var carTypesRepo = MockRepositoryFactory.Create<CarType, long>(_carTypes, ct => ct.CarTypeId);
        var customersRepo = MockRepositoryFactory.Create<Customer, long>(_customers, c => c.CustomerId);
        var hubsRepo = MockRepositoryFactory.Create<Hub, long>(_hubs, h => h.HubId);
        var addonsRepo = MockRepositoryFactory.Create<Addon, long>(_addons, a => a.AddonId);
        var carsRepo = MockRepositoryFactory.Create<Car, long>(_cars, c => c.CarId);

        _availabilityMock = new Mock<IAvailabilityRepository>();
        _availabilityMock.Setup(a => a.CountCapacityAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(5);
        _availabilityMock.Setup(a => a.CountOverlappingAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
            .ReturnsAsync(0);

        _vehicleServiceMock = new Mock<IVehicleService>();
        _vehicleServiceMock.Setup(v => v.GetCarTypeByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CarTypeDTO { CarTypeId = id, CarTypeName = "Sedan", DailyRate = 100.0 });
        _vehicleServiceMock.Setup(v => v.GetCarByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CarDTO { CarId = id, VehicleNumber = "MH01AA1111" });

        _customerServiceMock = new Mock<ICustomerService>();
        _customerServiceMock.Setup(c => c.GetCustomerByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CustomerDTO { CustomerId = id, FullName = "Jane Doe", Email = "jane@example.com" });

        _locationServiceMock = new Mock<ILocationService>();
        _locationServiceMock.Setup(l => l.GetHubByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new HubDTO { HubId = id, HubName = $"Hub {id}" });

        _addonServiceMock = new Mock<IAddonService>();
        _addonServiceMock.Setup(a => a.GetAddonsAsync())
            .ReturnsAsync((IReadOnlyList<AddonDTO>)new List<AddonDTO> { new() { AddonId = 1, AddonName = "GPS", DailyRate = 10.0 } });

        _emailServiceMock = new Mock<IEmailService>();

        _currentUserMock = new Mock<IAuthenticatedUserAccessor>();
        _currentUserMock.SetupGet(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.GetCurrentRole()).Returns("STAFF");
        _currentUserMock.Setup(c => c.GetCurrentUserId()).Returns(1);

        _service = new BookingService(
            bookingsRepo.Object,
            bookingDetailsRepo.Object,
            carTypesRepo.Object,
            customersRepo.Object,
            hubsRepo.Object,
            addonsRepo.Object,
            carsRepo.Object,
            _availabilityMock.Object,
            _vehicleServiceMock.Object,
            _customerServiceMock.Object,
            _locationServiceMock.Object,
            _addonServiceMock.Object,
            _emailServiceMock.Object,
            _currentUserMock.Object,
            NullLogger<BookingService>.Instance);
    }

    private static CreateBookingRequest MakeCreateRequest() => new()
    {
        CustomerId = 1,
        CarTypeId = 1,
        PickupHubId = 1,
        DropHubId = 2,
        PickupDatetime = new DateTime(2026, 8, 10),
        ReturnDatetime = new DateTime(2026, 8, 15),
    };

    private async Task<BookingHeader> SeedActiveBookingAsync(long? carId = null)
    {
        var response = await _service.CreateBookingAsync(MakeCreateRequest());
        var header = _bookings.Single(b => b.ConfirmationNo == response.ConfirmationNo);
        if (carId is not null)
        {
            header.CarId = carId;
        }
        return header;
    }

    // ---------- CreateBookingAsync ----------

    [Test]
    public async Task CreateBookingAsync_HappyPath_CreatesConfirmedBookingWithEstimatedAmount()
    {
        var result = await _service.CreateBookingAsync(MakeCreateRequest());

        result.BookingStatus.Should().Be("CONFIRMED");
        result.ConfirmationNo.Should().HaveLength(8);
        // 5 days * 100 daily rate + 0 addons (no addons requested)
        result.EstimatedAmount.Should().Be(500.0);
        _bookings.Should().ContainSingle();
    }

    [Test]
    public async Task CreateBookingAsync_WithAddons_AddsAddonAmountToEstimate()
    {
        var request = MakeCreateRequest();
        request.Addons = new List<AddonLineRequest> { new() { AddonId = 1, Quantity = 2 } };

        var result = await _service.CreateBookingAsync(request);

        // 5 days * 100 daily + (10 rate * 2 qty * 5 days) addon = 500 + 100
        result.EstimatedAmount.Should().Be(600.0);
        _bookingDetails.Should().ContainSingle();
    }

    [Test]
    public async Task CreateBookingAsync_CapacityExceeded_ThrowsConflict()
    {
        _availabilityMock.Setup(a => a.CountCapacityAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(1);
        _availabilityMock.Setup(a => a.CountOverlappingAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
            .ReturnsAsync(1);

        var act = async () => await _service.CreateBookingAsync(MakeCreateRequest());

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
        _bookings.Should().BeEmpty();
    }

    [Test]
    public async Task CreateBookingAsync_ReturnBeforePickup_ThrowsBadRequest()
    {
        var request = MakeCreateRequest();
        request.ReturnDatetime = request.PickupDatetime!.Value.AddDays(-1);

        var act = async () => await _service.CreateBookingAsync(request);

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateBookingAsync_UnknownCustomer_ThrowsResourceNotFound()
    {
        var request = MakeCreateRequest();
        request.CustomerId = 999;

        var act = async () => await _service.CreateBookingAsync(request);

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ---------- ModifyBookingAsync ----------

    [Test]
    public async Task ModifyBookingAsync_NullAddons_KeepsExistingAddonLines()
    {
        var createRequest = MakeCreateRequest();
        createRequest.Addons = new List<AddonLineRequest> { new() { AddonId = 1, Quantity = 1 } };
        var created = await _service.CreateBookingAsync(createRequest);

        var modifyRequest = new ModifyBookingRequest { Remarks = "no addon change" };
        var result = await _service.ModifyBookingAsync(created.ConfirmationNo, modifyRequest);

        _bookingDetails.Should().ContainSingle();
        result.EstimatedAmount.Should().Be(created.EstimatedAmount);
    }

    [Test]
    public async Task ModifyBookingAsync_EmptyAddonsList_RemovesAllAddonLines()
    {
        var createRequest = MakeCreateRequest();
        createRequest.Addons = new List<AddonLineRequest> { new() { AddonId = 1, Quantity = 1 } };
        var created = await _service.CreateBookingAsync(createRequest);

        var modifyRequest = new ModifyBookingRequest { Addons = new List<AddonLineRequest>() };
        var result = await _service.ModifyBookingAsync(created.ConfirmationNo, modifyRequest);

        _bookingDetails.Should().BeEmpty();
        result.EstimatedAmount.Should().Be(500.0);
    }

    [Test]
    public async Task ModifyBookingAsync_ChangePickupHubAfterCarAssigned_ThrowsConflict()
    {
        var header = await SeedActiveBookingAsync(carId: 1);

        var act = async () => await _service.ModifyBookingAsync(header.ConfirmationNo, new ModifyBookingRequest { PickupHubId = 2 });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ModifyBookingAsync_ChangePickupDatetimeAfterCarAssigned_ThrowsConflict()
    {
        var header = await SeedActiveBookingAsync(carId: 1);

        var act = async () => await _service.ModifyBookingAsync(
            header.ConfirmationNo, new ModifyBookingRequest { PickupDatetime = header.PickupDatetime.AddDays(1) });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ModifyBookingAsync_ChangeCategoryAfterCarAssigned_ThrowsConflict()
    {
        _carTypes.Add(new CarType { CarTypeId = 2, CarTypeName = "SUV", DailyRate = 150.0 });
        var header = await SeedActiveBookingAsync(carId: 1);

        var act = async () => await _service.ModifyBookingAsync(header.ConfirmationNo, new ModifyBookingRequest { CarTypeId = 2 });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ModifyBookingAsync_AddonsChangeAfterCarAssigned_ThrowsConflict()
    {
        var header = await SeedActiveBookingAsync(carId: 1);

        var act = async () => await _service.ModifyBookingAsync(
            header.ConfirmationNo, new ModifyBookingRequest { Addons = new List<AddonLineRequest>() });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ModifyBookingAsync_CategoryChangeBeforeCarAssigned_ReChecksCapacity()
    {
        _carTypes.Add(new CarType { CarTypeId = 2, CarTypeName = "SUV", DailyRate = 150.0 });
        var created = await _service.CreateBookingAsync(MakeCreateRequest());

        var result = await _service.ModifyBookingAsync(created.ConfirmationNo, new ModifyBookingRequest { CarTypeId = 2 });

        result.CarType!.CarTypeId.Should().Be(2);
        _availabilityMock.Verify(
            a => a.CountOverlappingAsync(1, 2, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ModifyBookingAsync_CapacityRecheckFails_ThrowsConflict()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());

        _availabilityMock.Setup(a => a.CountCapacityAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(1);
        _availabilityMock.Setup(a => a.CountOverlappingAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
            .ReturnsAsync(1);

        var act = async () => await _service.ModifyBookingAsync(
            created.ConfirmationNo, new ModifyBookingRequest { PickupDatetime = new DateTime(2026, 8, 11) });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ModifyBookingAsync_UnknownConfirmationNo_ThrowsResourceNotFound()
    {
        var act = async () => await _service.ModifyBookingAsync("NOPE0000", new ModifyBookingRequest());

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ---------- CancelBookingAsync ----------

    [Test]
    public async Task CancelBookingAsync_CarAssigned_ReleasesCarBackToAvailable()
    {
        var header = await SeedActiveBookingAsync(carId: 1);
        _cars.Single().Status = CarStatus.BOOKED;
        _cars.Single().IsAvailable = false;

        var result = await _service.CancelBookingAsync(header.ConfirmationNo);

        result.BookingStatus.Should().Be("CANCELLED");
        _cars.Single().Status.Should().Be(CarStatus.AVAILABLE);
        _cars.Single().IsAvailable.Should().BeTrue();
    }

    [Test]
    public async Task CancelBookingAsync_NoCarAssigned_DoesNotTouchAnyCar()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());
        var originalCarStatus = _cars.Single().Status;

        var result = await _service.CancelBookingAsync(created.ConfirmationNo);

        result.BookingStatus.Should().Be("CANCELLED");
        _cars.Single().Status.Should().Be(originalCarStatus);
    }

    [Test]
    public async Task CancelBookingAsync_UnknownConfirmationNo_ThrowsResourceNotFound()
    {
        var act = async () => await _service.CancelBookingAsync("NOPE0000");

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ---------- Access control ----------

    [Test]
    public async Task GetBookingByConfirmationAsync_CustomerAccessingSomeoneElsesBooking_ThrowsForbidden()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());

        _currentUserMock.Setup(c => c.GetCurrentRole()).Returns("CUSTOMER");
        _currentUserMock.Setup(c => c.GetCurrentUserId()).Returns(999);

        var act = async () => await _service.GetBookingByConfirmationAsync(created.ConfirmationNo);

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetBookingByConfirmationAsync_OwningCustomer_Succeeds()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());

        _currentUserMock.Setup(c => c.GetCurrentRole()).Returns("CUSTOMER");
        _currentUserMock.Setup(c => c.GetCurrentUserId()).Returns(1);

        var result = await _service.GetBookingByConfirmationAsync(created.ConfirmationNo);

        result.ConfirmationNo.Should().Be(created.ConfirmationNo);
    }

    // ---------- Read helpers ----------

    [Test]
    public async Task GetLastBookingForCustomerAsync_NoBookings_ReturnsNull()
    {
        var result = await _service.GetLastBookingForCustomerAsync(1);

        result.Should().BeNull();
    }

    [Test]
    public async Task GetBookingsForCustomerAsync_ExcludesCancelledBookings()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());
        await _service.CancelBookingAsync(created.ConfirmationNo);

        var result = await _service.GetBookingsForCustomerAsync(1);

        result.Should().BeEmpty();
    }
}
