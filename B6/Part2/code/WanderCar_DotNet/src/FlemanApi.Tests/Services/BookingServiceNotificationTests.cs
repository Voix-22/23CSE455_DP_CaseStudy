using FlemanApi.DTO;
using FlemanApi.Exceptions;
using FlemanApi.Models;
using FlemanApi.Repository;
using FlemanApi.Security;
using FlemanApi.Service;
using FlemanApi.Service.Notifications;
using FlemanApi.Tests.TestHelpers;
using FlemanApi.Util;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FlemanApi.Tests.Services;

// End-to-end CH04 wiring: BookingService -> BookingEventPublisher ->
// EmailBookingObserver -> real EmailService -> (mocked) background queue,
// plus AuditLogBookingObserver, in the same order as Program.cs.
[TestFixture]
public class BookingServiceNotificationTests
{
    private List<BookingHeader> _bookings = null!;
    private Mock<IEmailBackgroundQueue> _queueMock = null!;
    private List<Func<IEmailSender, CancellationToken, Task>> _queued = null!;
    private ListLogger<AuditLogBookingObserver> _auditLogger = null!;
    private Func<IBookingEventPublisher, BookingService> _buildService = null!;
    private BookingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _bookings = new List<BookingHeader>();
        var bookingDetails = new List<BookingDetail>();
        var carTypes = new List<CarType> { new() { CarTypeId = 1, CarTypeName = "Sedan", DailyRate = 100.0 } };
        var customers = new List<Customer> { new() { CustomerId = 1, FullName = "Jane Doe", Email = "jane@example.com" } };
        var hubs = new List<Hub>
        {
            new() { HubId = 1, HubName = "Pickup Hub", CityId = 1, StateId = 1, Pincode = "400001", ContactNo = "111" },
            new() { HubId = 2, HubName = "Drop Hub", CityId = 1, StateId = 1, Pincode = "400002", ContactNo = "222" },
        };

        var availability = new Mock<IAvailabilityRepository>();
        availability.Setup(a => a.CountCapacityAsync(It.IsAny<long>(), It.IsAny<long>())).ReturnsAsync(5);
        availability.Setup(a => a.CountOverlappingAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<long?>()))
            .ReturnsAsync(0);

        var vehicleService = new Mock<IVehicleService>();
        vehicleService.Setup(v => v.GetCarTypeByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CarTypeDTO { CarTypeId = id, CarTypeName = "Sedan", DailyRate = 100.0 });
        var customerService = new Mock<ICustomerService>();
        customerService.Setup(c => c.GetCustomerByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new CustomerDTO { CustomerId = id, FullName = "Jane Doe", Email = "jane@example.com" });
        var locationService = new Mock<ILocationService>();
        locationService.Setup(l => l.GetHubByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((long id) => new HubDTO { HubId = id, HubName = $"Hub {id}" });
        var addonService = new Mock<IAddonService>();
        addonService.Setup(a => a.GetAddonsAsync()).ReturnsAsync((IReadOnlyList<AddonDTO>)new List<AddonDTO>());

        var currentUser = new Mock<IAuthenticatedUserAccessor>();
        currentUser.SetupGet(c => c.IsAuthenticated).Returns(true);
        currentUser.Setup(c => c.GetCurrentRole()).Returns("STAFF");
        currentUser.Setup(c => c.GetCurrentUserId()).Returns(1);

        _queued = new List<Func<IEmailSender, CancellationToken, Task>>();
        _queueMock = new Mock<IEmailBackgroundQueue>();
        _queueMock.Setup(q => q.Enqueue(It.IsAny<Func<IEmailSender, CancellationToken, Task>>()))
            .Callback<Func<IEmailSender, CancellationToken, Task>>(item => _queued.Add(item));

        // Integration with the pricing-policy change: BookingService now also
        // takes a selector. Standard pricing keeps the amounts asserted below.
        var pricingPolicySelector = new Mock<IPricingPolicySelector>();
        pricingPolicySelector
            .Setup(s => s.SelectPolicy(It.IsAny<CarType>(), It.IsAny<DateTime>(), It.IsAny<DateTime>()))
            .Returns(new StandardPricingPolicy());

        _auditLogger = new ListLogger<AuditLogBookingObserver>();
        _buildService = publisher => new BookingService(
            MockRepositoryFactory.Create<BookingHeader, long>(_bookings, b => b.BookingId, (b, id) => b.BookingId = id).Object,
            MockRepositoryFactory.Create<BookingDetail, long>(bookingDetails, d => d.BookingDetailId, (d, id) => d.BookingDetailId = id).Object,
            MockRepositoryFactory.Create<CarType, long>(carTypes, ct => ct.CarTypeId).Object,
            MockRepositoryFactory.Create<Customer, long>(customers, c => c.CustomerId).Object,
            MockRepositoryFactory.Create<Hub, long>(hubs, h => h.HubId).Object,
            MockRepositoryFactory.Create<Addon, long>(new List<Addon>(), a => a.AddonId).Object,
            MockRepositoryFactory.Create<Car, long>(new List<Car>(), c => c.CarId).Object,
            availability.Object,
            vehicleService.Object,
            customerService.Object,
            locationService.Object,
            addonService.Object,
            publisher,
            currentUser.Object,
            NullLogger<BookingService>.Instance,
            pricingPolicySelector.Object);

        _service = _buildService(new BookingEventPublisher(
            new IBookingEventObserver[] { MakeEmailObserver(), new AuditLogBookingObserver(_auditLogger) },
            NullLogger<BookingEventPublisher>.Instance));
    }

    private EmailBookingObserver MakeEmailObserver() => new(new EmailService(_queueMock.Object));

    private static CreateBookingRequest MakeCreateRequest() => new()
    {
        CustomerId = 1,
        CarTypeId = 1,
        PickupHubId = 1,
        DropHubId = 2,
        PickupDatetime = new DateTime(2026, 8, 10),
        ReturnDatetime = new DateTime(2026, 8, 15),
    };

    // Runs every queued work item against a mock sender, i.e. what
    // EmailQueueHostedService would do, and returns that sender.
    private async Task<Mock<IEmailSender>> DrainQueueAsync()
    {
        var sender = new Mock<IEmailSender>();
        foreach (var item in _queued) await item(sender.Object, CancellationToken.None);
        return sender;
    }

    [Test]
    public async Task CreateBookingAsync_QueuesOneConfirmationEmail_WithBaselineRecipientSubjectAndBody()
    {
        var result = await _service.CreateBookingAsync(MakeCreateRequest());

        var header = _bookings.Single();
        var baselineBody = $"""
                    Dear Jane Doe,

                    Your booking has been confirmed successfully.

                    Booking Details
                    -------------------------
                    Booking No : {header.ConfirmationNo}
                    Vehicle Type : Sedan
                    Pickup Date : {header.PickupDatetime}
                    Return Date : {header.ReturnDatetime}
                    Total Amount : ₹{header.EstimatedAmount:F2}

                    Thank you for choosing Fleeman.

                    Regards,
                    Fleeman Team
                    """;

        result.ConfirmationNo.Should().Be(header.ConfirmationNo);
        _queued.Should().ContainSingle();
        var sender = await DrainQueueAsync();
        sender.Verify(s => s.SendPlainAsync("jane@example.com", "Booking Confirmation - Fleeman", baselineBody), Times.Once);
        _auditLogger.Entries.Should().ContainSingle().Which.Properties["EventKind"].Should().Be(BookingEventKind.CREATED);
    }

    [Test]
    public async Task ModifyBookingAsync_QueuesOneUpdateEmail_AndWritesOneAuditEntry()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());
        _queued.Clear();
        _auditLogger.Entries.Clear();

        await _service.ModifyBookingAsync(created.ConfirmationNo, new ModifyBookingRequest { ReturnDatetime = new DateTime(2026, 8, 17) });

        _queued.Should().ContainSingle();
        var sender = await DrainQueueAsync();
        sender.Verify(s => s.SendPlainAsync("jane@example.com", "Booking Updated - Fleeman", It.IsAny<string>()), Times.Once);
        var audit = _auditLogger.Entries.Should().ContainSingle().Subject;
        audit.Properties["EventKind"].Should().Be(BookingEventKind.MODIFIED);
        audit.Properties["ConfirmationNo"].Should().Be(created.ConfirmationNo);
    }

    [Test]
    public async Task CancelBookingAsync_QueuesOneCancellationEmail_AndWritesOneAuditEntry()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());
        _queued.Clear();
        _auditLogger.Entries.Clear();

        await _service.CancelBookingAsync(created.ConfirmationNo);

        _queued.Should().ContainSingle();
        var sender = await DrainQueueAsync();
        sender.Verify(s => s.SendPlainAsync("jane@example.com", "Booking Cancelled - Fleeman", It.IsAny<string>()), Times.Once);
        var audit = _auditLogger.Entries.Should().ContainSingle().Subject;
        audit.Properties["EventKind"].Should().Be(BookingEventKind.CANCELLED);
        audit.Properties["BookingStatus"].Should().Be(BookingStatus.CANCELLED);
    }

    private sealed class ThrowingObserver : IBookingEventObserver
    {
        public Task OnBookingEventAsync(BookingEvent bookingEvent) => throw new InvalidOperationException("boom");
    }

    [Test]
    public async Task CreateBookingAsync_ObserverThrows_BookingStillSucceeds_AndLaterObserversRun()
    {
        // The failing observer is first, so the e-mail and audit observers
        // only run if the publisher isolates the failure.
        var service = _buildService(new BookingEventPublisher(
            new IBookingEventObserver[] { new ThrowingObserver(), MakeEmailObserver(), new AuditLogBookingObserver(_auditLogger) },
            NullLogger<BookingEventPublisher>.Instance));

        var result = await service.CreateBookingAsync(MakeCreateRequest());

        result.BookingStatus.Should().Be("CONFIRMED");
        _bookings.Should().ContainSingle();
        _queued.Should().ContainSingle();
        _auditLogger.Entries.Should().ContainSingle();
    }

    [Test]
    public async Task ModifyBookingAsync_Rejected_PublishesNoEvent()
    {
        var created = await _service.CreateBookingAsync(MakeCreateRequest());
        _queued.Clear();
        _auditLogger.Entries.Clear();

        var act = () => _service.ModifyBookingAsync(created.ConfirmationNo, new ModifyBookingRequest { PickupHubId = 99 });

        await act.Should().ThrowAsync<ResourceNotFoundException>();
        _queued.Should().BeEmpty();
        _auditLogger.Entries.Should().BeEmpty();
    }
}
