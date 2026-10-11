using FlemanApi.Data;
using FlemanApi.DTO;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Service;
using FlemanApi.Tests.TestHelpers;
using FlemanApi.Util;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlemanApi.Tests.Services;

// Acceptance criterion 1 of CH03: the existing fleet-availability code reacts
// to maintenance with no change of its own. The unchanged AvailabilityRepository
// and StaffService read the same InMemory database MaintenanceService writes to.
[TestFixture]
public class MaintenanceFleetAvailabilityTests
{
    private static readonly DateTime FixedNow = new(2026, 10, 10, 9, 30, 0);

    private FlemanDbContext _context = null!;
    private AvailabilityRepository _availability = null!;
    private StaffService _staffService = null!;
    private MaintenanceService _maintenanceService = null!;

    [SetUp]
    public async Task SetUp()
    {
        var options = new DbContextOptionsBuilder<FlemanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FlemanDbContext(options);

        _context.Cars.AddRange(
            new Car { CarId = 1, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE, IsAvailable = true },
            new Car { CarId = 2, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA2222", Status = CarStatus.AVAILABLE, IsAvailable = true });
        _context.BookingHeaders.Add(new BookingHeader
        {
            BookingId = 1,
            ConfirmationNo = "ABC12345",
            BookingDate = FixedNow,
            CustomerId = 1,
            CarTypeId = 1,
            PickupHubId = 1,
            DropHubId = 1,
            PickupDatetime = FixedNow.AddDays(1),
            ReturnDatetime = FixedNow.AddDays(3),
            BookingStatus = BookingStatus.CONFIRMED,
        });
        await _context.SaveChangesAsync();

        var clock = new FixedTimeProvider(FixedNow);
        var cars = new GenericRepository<Car, long>(_context);
        _availability = new AvailabilityRepository(_context);

        var vehicleService = new Mock<IVehicleService>();
        vehicleService.Setup(v => v.GetCarByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CarDTO { CarId = id });

        _staffService = new StaffService(
            new GenericRepository<BookingHeader, long>(_context),
            new GenericRepository<BookingDetail, long>(_context),
            cars,
            new GenericRepository<CarType, long>(_context),
            new GenericRepository<Customer, long>(_context),
            new GenericRepository<Addon, long>(_context),
            new GenericRepository<InvoiceHeader, long>(_context),
            new GenericRepository<InvoiceDetail, long>(_context),
            _availability,
            Mock.Of<IBookingService>(),
            vehicleService.Object,
            Mock.Of<IEmailService>(),
            Mock.Of<IInvoicePdfService>(),
            NullLogger<StaffService>.Instance,
            Mock.Of<IPricingPolicySelector>(),
            clock);

        _maintenanceService = new MaintenanceService(
            new GenericRepository<MaintenanceSchedule, long>(_context), cars, clock);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private async Task<long[]> HandoverPickerCarIdsAsync() =>
        (await _staffService.GetAvailableCarsForHandoverAsync("ABC12345")).Select(c => c.CarId).OrderBy(id => id).ToArray();

    private async Task<MaintenanceScheduleDTO> ScheduleForCar1Async() =>
        await _maintenanceService.ScheduleAsync(new ScheduleMaintenanceRequest
        {
            CarId = 1,
            MaintenanceType = MaintenanceType.REPAIR,
            ScheduledDate = FixedNow,
        });

    [Test]
    public async Task StartingMaintenance_RemovesCarFromCapacityAndHandoverPicker()
    {
        var schedule = await ScheduleForCar1Async();

        (await _availability.CountCapacityAsync(1, 1)).Should().Be(2, "scheduling alone changes nothing");
        (await HandoverPickerCarIdsAsync()).Should().Equal(1, 2);

        await _maintenanceService.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        (await _availability.CountCapacityAsync(1, 1)).Should().Be(1);
        (await HandoverPickerCarIdsAsync()).Should().Equal(2);
        var dashboard = (await _availability.CountByCarTypeAsync(1)).Single();
        dashboard.AvailableCars.Should().Be(1);
        dashboard.MaintenanceCars.Should().Be(1);
    }

    [Test]
    public async Task CompletingMaintenance_PutsCarBackIntoCapacityAndHandoverPicker()
    {
        var schedule = await ScheduleForCar1Async();
        await _maintenanceService.StartAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        await _maintenanceService.CompleteAsync(schedule.MaintenanceScheduleId, new MaintenanceTransitionRequest());

        (await _availability.CountCapacityAsync(1, 1)).Should().Be(2);
        (await HandoverPickerCarIdsAsync()).Should().Equal(1, 2);
        var dashboard = (await _availability.CountByCarTypeAsync(1)).Single();
        dashboard.AvailableCars.Should().Be(2);
        dashboard.MaintenanceCars.Should().Be(0);
    }
}
