using FlemanApi.Data;
using FlemanApi.Models;
using FlemanApi.Repository;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FlemanApi.Tests.Repository;

[TestFixture]
public class AvailabilityRepositoryTests
{
    private FlemanDbContext _context = null!;
    private AvailabilityRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<FlemanDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _context = new FlemanDbContext(options);
        _repository = new AvailabilityRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    [Test]
    public async Task CountCapacityAsync_ExcludesUnderMaintenanceCars()
    {
        _context.Cars.AddRange(
            new Car { CarId = 1, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE },
            new Car { CarId = 2, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA2222", Status = CarStatus.BOOKED },
            new Car { CarId = 3, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA3333", Status = CarStatus.UNDER_MAINTENANCE },
            new Car { CarId = 4, HubId = 2, CarTypeId = 1, VehicleNumber = "MH01AA4444", Status = CarStatus.AVAILABLE });
        await _context.SaveChangesAsync();

        var capacity = await _repository.CountCapacityAsync(1, 1);

        capacity.Should().Be(2);
    }

    [Test]
    public async Task CountOverlappingAsync_OverlappingActiveBooking_IsCounted()
    {
        _context.BookingHeaders.Add(new BookingHeader
        {
            ConfirmationNo = "ABC12345",
            BookingDate = DateTime.Now,
            CustomerId = 1,
            CarTypeId = 1,
            PickupHubId = 1,
            DropHubId = 1,
            PickupDatetime = new DateTime(2026, 8, 10),
            ReturnDatetime = new DateTime(2026, 8, 15),
            BookingStatus = BookingStatus.CONFIRMED,
        });
        await _context.SaveChangesAsync();

        var overlapping = await _repository.CountOverlappingAsync(
            1, 1, new DateTime(2026, 8, 12), new DateTime(2026, 8, 20), null);

        overlapping.Should().Be(1);
    }

    [Test]
    public async Task CountOverlappingAsync_NonOverlappingDateRange_IsNotCounted()
    {
        _context.BookingHeaders.Add(new BookingHeader
        {
            ConfirmationNo = "ABC12345",
            BookingDate = DateTime.Now,
            CustomerId = 1,
            CarTypeId = 1,
            PickupHubId = 1,
            DropHubId = 1,
            PickupDatetime = new DateTime(2026, 8, 10),
            ReturnDatetime = new DateTime(2026, 8, 15),
            BookingStatus = BookingStatus.CONFIRMED,
        });
        await _context.SaveChangesAsync();

        var overlapping = await _repository.CountOverlappingAsync(
            1, 1, new DateTime(2026, 8, 16), new DateTime(2026, 8, 20), null);

        overlapping.Should().Be(0);
    }

    [Test]
    public async Task CountOverlappingAsync_CancelledBooking_IsNotCounted()
    {
        _context.BookingHeaders.Add(new BookingHeader
        {
            ConfirmationNo = "ABC12345",
            BookingDate = DateTime.Now,
            CustomerId = 1,
            CarTypeId = 1,
            PickupHubId = 1,
            DropHubId = 1,
            PickupDatetime = new DateTime(2026, 8, 10),
            ReturnDatetime = new DateTime(2026, 8, 15),
            BookingStatus = BookingStatus.CANCELLED,
        });
        await _context.SaveChangesAsync();

        var overlapping = await _repository.CountOverlappingAsync(
            1, 1, new DateTime(2026, 8, 10), new DateTime(2026, 8, 15), null);

        overlapping.Should().Be(0);
    }

    [Test]
    public async Task CountOverlappingAsync_ExcludeBookingId_ExcludesThatBooking()
    {
        _context.BookingHeaders.Add(new BookingHeader
        {
            BookingId = 42,
            ConfirmationNo = "ABC12345",
            BookingDate = DateTime.Now,
            CustomerId = 1,
            CarTypeId = 1,
            PickupHubId = 1,
            DropHubId = 1,
            PickupDatetime = new DateTime(2026, 8, 10),
            ReturnDatetime = new DateTime(2026, 8, 15),
            BookingStatus = BookingStatus.CONFIRMED,
        });
        await _context.SaveChangesAsync();

        var overlapping = await _repository.CountOverlappingAsync(
            1, 1, new DateTime(2026, 8, 10), new DateTime(2026, 8, 15), 42);

        overlapping.Should().Be(0);
    }

    [Test]
    public async Task CountByCarTypeAsync_GroupsCountsByCarType()
    {
        _context.Cars.AddRange(
            new Car { CarId = 1, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE },
            new Car { CarId = 2, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA2222", Status = CarStatus.BOOKED },
            new Car { CarId = 3, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA3333", Status = CarStatus.UNDER_MAINTENANCE },
            new Car { CarId = 4, HubId = 1, CarTypeId = 2, VehicleNumber = "MH01AA4444", Status = CarStatus.AVAILABLE });
        await _context.SaveChangesAsync();

        var counts = (await _repository.CountByCarTypeAsync(null)).OrderBy(c => c.CarTypeId).ToList();

        counts.Should().HaveCount(2);
        counts[0].CarTypeId.Should().Be(1);
        counts[0].TotalCars.Should().Be(3);
        counts[0].AvailableCars.Should().Be(1);
        counts[0].BookedCars.Should().Be(1);
        counts[0].MaintenanceCars.Should().Be(1);
        counts[1].CarTypeId.Should().Be(2);
        counts[1].TotalCars.Should().Be(1);
    }

    [Test]
    public async Task CountByCarTypeAsync_ScopedToHub_OnlyCountsThatHub()
    {
        _context.Cars.AddRange(
            new Car { CarId = 1, HubId = 1, CarTypeId = 1, VehicleNumber = "MH01AA1111", Status = CarStatus.AVAILABLE },
            new Car { CarId = 2, HubId = 2, CarTypeId = 1, VehicleNumber = "MH01AA2222", Status = CarStatus.AVAILABLE });
        await _context.SaveChangesAsync();

        var counts = await _repository.CountByCarTypeAsync(1);

        counts.Should().ContainSingle();
        counts[0].TotalCars.Should().Be(1);
    }
}
