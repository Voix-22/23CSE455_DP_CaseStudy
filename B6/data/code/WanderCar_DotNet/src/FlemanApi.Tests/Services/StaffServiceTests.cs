using System.Net;
using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class StaffServiceTests
{
    private List<BookingHeader> _bookings = null!;
    private List<BookingDetail> _bookingDetails = null!;
    private List<Car> _cars = null!;
    private List<CarType> _carTypes = null!;
    private List<Customer> _customers = null!;
    private List<Addon> _addons = null!;
    private List<InvoiceHeader> _invoiceHeaders = null!;
    private List<InvoiceDetail> _invoiceDetails = null!;

    private Mock<IAvailabilityRepository> _availabilityMock = null!;
    private Mock<IBookingService> _bookingServiceMock = null!;
    private Mock<IVehicleService> _vehicleServiceMock = null!;
    private Mock<IEmailService> _emailServiceMock = null!;
    private Mock<IInvoicePdfService> _invoicePdfServiceMock = null!;

    private StaffService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bookings = new List<BookingHeader>
        {
            new()
            {
                BookingId = 1,
                ConfirmationNo = "ABC12345",
                BookingDate = DateTime.Now,
                CustomerId = 1,
                CarTypeId = 1,
                PickupHubId = 1,
                DropHubId = 1,
                PickupDatetime = new DateTime(2026, 8, 1),
                ReturnDatetime = new DateTime(2026, 8, 5),
                BookingStatus = BookingStatus.CONFIRMED,
            },
        };
        _bookingDetails = new List<BookingDetail>();
        _cars = new List<Car>
        {
            new() { CarId = 1, CarTypeId = 1, HubId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE, Odometer = 1000, FuelLevel = 100 },
        };
        _carTypes = new List<CarType> { new() { CarTypeId = 1, CarTypeName = "Sedan", DailyRate = 100.0 } };
        _customers = new List<Customer> { new() { CustomerId = 1, FullName = "Jane Doe", Email = "jane@example.com" } };
        _addons = new List<Addon> { new() { AddonId = 1, AddonName = "GPS", DailyRate = 10.0 } };
        _invoiceHeaders = new List<InvoiceHeader>();
        _invoiceDetails = new List<InvoiceDetail>();

        var bookingsRepo = MockRepositoryFactory.Create<BookingHeader, long>(_bookings, b => b.BookingId);
        var bookingDetailsRepo = MockRepositoryFactory.Create<BookingDetail, long>(_bookingDetails, d => d.BookingDetailId, (d, id) => d.BookingDetailId = id);
        var carsRepo = MockRepositoryFactory.Create<Car, long>(_cars, c => c.CarId);
        var carTypesRepo = MockRepositoryFactory.Create<CarType, long>(_carTypes, ct => ct.CarTypeId);
        var customersRepo = MockRepositoryFactory.Create<Customer, long>(_customers, c => c.CustomerId);
        var addonsRepo = MockRepositoryFactory.Create<Addon, long>(_addons, a => a.AddonId);
        var invoiceHeadersRepo = MockRepositoryFactory.Create<InvoiceHeader, long>(_invoiceHeaders, i => i.InvoiceId, (i, id) => i.InvoiceId = id);
        var invoiceDetailsRepo = MockRepositoryFactory.Create<InvoiceDetail, long>(_invoiceDetails, d => d.InvoiceDetailId, (d, id) => d.InvoiceDetailId = id);

        _availabilityMock = new Mock<IAvailabilityRepository>();

        _bookingServiceMock = new Mock<IBookingService>();
        _bookingServiceMock.Setup(b => b.GetBookingByConfirmationAsync(It.IsAny<string>()))
            .ReturnsAsync((string confNo) => new BookingResponseDTO { ConfirmationNo = confNo });

        _vehicleServiceMock = new Mock<IVehicleService>();
        _vehicleServiceMock.Setup(v => v.GetCarByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CarDTO { CarId = id, VehicleNumber = "MH01AA1111" });

        _emailServiceMock = new Mock<IEmailService>();
        _invoicePdfServiceMock = new Mock<IInvoicePdfService>();
        _invoicePdfServiceMock.Setup(p => p.GenerateInvoiceAsync(It.IsAny<long>())).ReturnsAsync(new byte[] { 1, 2, 3 });

        _service = new StaffService(
            bookingsRepo.Object,
            bookingDetailsRepo.Object,
            carsRepo.Object,
            carTypesRepo.Object,
            customersRepo.Object,
            addonsRepo.Object,
            invoiceHeadersRepo.Object,
            invoiceDetailsRepo.Object,
            _availabilityMock.Object,
            _bookingServiceMock.Object,
            _vehicleServiceMock.Object,
            _emailServiceMock.Object,
            _invoicePdfServiceMock.Object,
            NullLogger<StaffService>.Instance);
    }

    private BookingHeader Booking => _bookings.Single();
    private Car Car => _cars.Single();

    // ---------- HandoverVehicleAsync ----------

    [Test]
    public async Task HandoverVehicleAsync_HappyPath_AssignsCarAndMarksOngoing()
    {
        var result = await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = 1, FuelStatus = 100 });

        Booking.CarId.Should().Be(1);
        Booking.BookingStatus.Should().Be(BookingStatus.ONGOING);
        Booking.HandoverFuelLevel.Should().Be(100);
        Car.Status.Should().Be(CarStatus.BOOKED);
        Car.IsAvailable.Should().BeFalse();
        result.Should().NotBeNull();
    }

    [Test]
    public async Task HandoverVehicleAsync_AlreadyHandedOver_ThrowsConflict()
    {
        Booking.CarId = 1;

        var act = async () => await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = 1 });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task HandoverVehicleAsync_NoCarIdInRequest_ThrowsBadRequest()
    {
        var act = async () => await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = null });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task HandoverVehicleAsync_CarNotFound_ThrowsResourceNotFound()
    {
        var act = async () => await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = 999 });

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    [Test]
    public async Task HandoverVehicleAsync_WrongHub_ThrowsConflict()
    {
        Car.HubId = 2;

        var act = async () => await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = 1 });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task HandoverVehicleAsync_WrongCategory_ThrowsConflict()
    {
        Car.CarTypeId = 2;

        var act = async () => await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = 1 });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task HandoverVehicleAsync_CarNotAvailable_ThrowsConflict()
    {
        Car.Status = CarStatus.BOOKED;

        var act = async () => await _service.HandoverVehicleAsync("ABC12345", new HandoverRequest { CarId = 1 });

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task HandoverVehicleAsync_UnknownConfirmationNo_ThrowsResourceNotFound()
    {
        var act = async () => await _service.HandoverVehicleAsync("NOPE0000", new HandoverRequest { CarId = 1 });

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ---------- ProcessReturnAsync ----------

    private void GivenHandedOverBooking(int handoverFuel = 100)
    {
        Booking.CarId = 1;
        Booking.ActualHandoverDatetime = new DateTime(2026, 8, 1);
        Booking.HandoverFuelLevel = handoverFuel;
        Car.Status = CarStatus.BOOKED;
        Car.IsAvailable = false;
    }

    [Test]
    public async Task ProcessReturnAsync_NotHandedOverYet_ThrowsConflict()
    {
        var act = async () => await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest());

        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.Status.Should().Be(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task ProcessReturnAsync_FuelReturnedLowerThanHandover_ChargesFuelShortfall()
    {
        GivenHandedOverBooking(handoverFuel: 100);

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { FuelStatus = 50 });

        // (100 - 50) / 25 = 2 quarters short * 2.0 per quarter = 4.0
        result.FuelCharge.Should().Be(4.0);
    }

    [Test]
    public async Task ProcessReturnAsync_FuelReturnedEqualToHandover_NoFuelCharge()
    {
        GivenHandedOverBooking(handoverFuel: 100);

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { FuelStatus = 100 });

        result.FuelCharge.Should().Be(0.0);
    }

    [Test]
    public async Task ProcessReturnAsync_FuelReturnedHigherThanHandover_NoCreditNoCharge()
    {
        GivenHandedOverBooking(handoverFuel: 50);

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { FuelStatus = 100 });

        result.FuelCharge.Should().Be(0.0);
    }

    [Test]
    public async Task ProcessReturnAsync_PartialQuarterShortfall_RoundsDownToWholeQuarters()
    {
        GivenHandedOverBooking(handoverFuel: 100);

        // 100 - 75 = 25 -> exactly 1 quarter short
        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { FuelStatus = 75 });

        result.FuelCharge.Should().Be(2.0);
    }

    [Test]
    public async Task ProcessReturnAsync_PaymentTypeGiven_PaymentStatusIsPaid()
    {
        GivenHandedOverBooking();

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { PaymentType = PaymentType.CASH });

        result.PaymentStatus.Should().Be("PAID");
    }

    [Test]
    public async Task ProcessReturnAsync_NoPaymentType_PaymentStatusIsPending()
    {
        GivenHandedOverBooking();

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { PaymentType = null });

        result.PaymentStatus.Should().Be("PENDING");
    }

    [Test]
    public async Task ProcessReturnAsync_ReleasesCarBackToAvailable()
    {
        GivenHandedOverBooking();

        await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest());

        Car.Status.Should().Be(CarStatus.AVAILABLE);
        Car.IsAvailable.Should().BeTrue();
    }

    [Test]
    public async Task ProcessReturnAsync_ExtraMiles_IncreasesOdometer()
    {
        GivenHandedOverBooking();
        Car.Odometer = 1000;

        await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { ExtraMiles = 50 });

        Car.Odometer.Should().Be(1050);
    }

    [Test]
    public async Task ProcessReturnAsync_NoExtraMiles_OdometerUnchanged()
    {
        GivenHandedOverBooking();
        Car.Odometer = 1000;

        await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest { ExtraMiles = null });

        Car.Odometer.Should().Be(1000);
    }

    [Test]
    public async Task ProcessReturnAsync_MarksBookingCompleted()
    {
        GivenHandedOverBooking();

        await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest());

        Booking.BookingStatus.Should().Be(BookingStatus.COMPLETED);
    }

    [Test]
    public async Task ProcessReturnAsync_CreatesInvoiceHeaderAndDetails()
    {
        GivenHandedOverBooking();
        _bookingDetails.Add(new BookingDetail { BookingDetailId = 1, BookingId = 1, AddonId = 1, AddonRate = 10.0, Quantity = 1, Subtotal = 40.0 });

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest());

        _invoiceHeaders.Should().ContainSingle();
        _invoiceDetails.Should().ContainSingle(d => d.AddonName == "GPS");
        result.AddonAmount.Should().Be(40.0);
    }

    [Test]
    public async Task ProcessReturnAsync_EmailSendThrows_StillReturnsInvoice()
    {
        GivenHandedOverBooking();
        _emailServiceMock.Setup(e => e.SendBookingInvoiceAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BookingResponseDTO>(), It.IsAny<InvoiceResponseDTO>(), It.IsAny<byte[]>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var result = await _service.ProcessReturnAsync("ABC12345", new ProcessReturnRequest());

        result.Should().NotBeNull();
    }

    [Test]
    public async Task ProcessReturnAsync_UnknownConfirmationNo_ThrowsResourceNotFound()
    {
        var act = async () => await _service.ProcessReturnAsync("NOPE0000", new ProcessReturnRequest());

        await act.Should().ThrowAsync<ResourceNotFoundException>();
    }

    // ---------- GetAvailableCarsForHandoverAsync ----------

    [Test]
    public async Task GetAvailableCarsForHandoverAsync_FiltersByHubAndCategoryAndAvailability()
    {
        _cars.Add(new Car { CarId = 2, CarTypeId = 1, HubId = 1, VehicleNumber = "MH01AA2222", Status = CarStatus.BOOKED });
        _cars.Add(new Car { CarId = 3, CarTypeId = 2, HubId = 1, VehicleNumber = "MH01AA3333", Status = CarStatus.AVAILABLE });

        var result = await _service.GetAvailableCarsForHandoverAsync("ABC12345");

        result.Should().ContainSingle(c => c.CarId == 1);
    }

    // ---------- GetDashboardAsync ----------

    [Test]
    public async Task GetDashboardAsync_MapsCarTypeCountsToDashboardDto()
    {
        _availabilityMock.Setup(a => a.CountByCarTypeAsync(It.IsAny<long?>()))
            .ReturnsAsync((IReadOnlyList<CarTypeCountProjection>)new List<CarTypeCountProjection>
            {
                new(1, 5, 3, 1, 1),
            });

        var result = await _service.GetDashboardAsync(null);

        result.Should().ContainSingle();
        result[0].CarTypeName.Should().Be("Sedan");
        result[0].TotalCars.Should().Be(5);
        result[0].AvailableCars.Should().Be(3);
    }
}
