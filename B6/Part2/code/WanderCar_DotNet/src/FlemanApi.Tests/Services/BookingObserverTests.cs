using FlemanApi.Models;
using FlemanApi.Service;
using FlemanApi.Service.Notifications;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class BookingObserverTests
{
    private static readonly DateTime Pickup = new(2026, 8, 10, 9, 30, 0);
    private static readonly DateTime Return = new(2026, 8, 15, 18, 0, 0);

    private static BookingEvent MakeEvent(BookingEventKind kind, string? email = "jane@example.com") => new(
        kind, 7, "ABC12345", 1, "Jane Doe", email, "Sedan", Pickup, Return, 510.0,
        kind == BookingEventKind.CANCELLED ? BookingStatus.CANCELLED : BookingStatus.CONFIRMED);

    // Copied verbatim from the baseline BookingService.CreateBookingAsync
    // (dfc61ab8, Service/BookingService.cs:172-189), with the header/customer/
    // carType expressions replaced by the same values the event carries.
    private static string BaselineConfirmationBody(string fullName, string confirmationNo, string carTypeName,
        DateTime pickup, DateTime returnDt, double? estimatedAmount) => $"""
                    Dear {fullName},

                    Your booking has been confirmed successfully.

                    Booking Details
                    -------------------------
                    Booking No : {confirmationNo}
                    Vehicle Type : {carTypeName}
                    Pickup Date : {pickup}
                    Return Date : {returnDt}
                    Total Amount : ₹{estimatedAmount:F2}

                    Thank you for choosing Fleeman.

                    Regards,
                    Fleeman Team
                    """;

    // ---------- EmailBookingObserver ----------

    [Test]
    public async Task EmailObserver_Created_SendsExactlyTheBaselineSubjectAndBody()
    {
        var email = new Mock<IEmailService>();
        var observer = new EmailBookingObserver(email.Object);

        await observer.OnBookingEventAsync(MakeEvent(BookingEventKind.CREATED));

        var expectedBody = BaselineConfirmationBody("Jane Doe", "ABC12345", "Sedan", Pickup, Return, 510.0);
        email.Verify(e => e.SendEmailAsync("jane@example.com", "Booking Confirmation - Fleeman", expectedBody), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EmailObserver_Modified_SendsOneUpdateEmail()
    {
        var email = new Mock<IEmailService>();
        var observer = new EmailBookingObserver(email.Object);

        await observer.OnBookingEventAsync(MakeEvent(BookingEventKind.MODIFIED));

        email.Verify(e => e.SendEmailAsync("jane@example.com", "Booking Updated - Fleeman",
            It.Is<string>(b => b.Contains("ABC12345") && b.Contains("has been updated") && b.Contains("₹510.00"))), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EmailObserver_Cancelled_SendsOneCancellationEmail()
    {
        var email = new Mock<IEmailService>();
        var observer = new EmailBookingObserver(email.Object);

        await observer.OnBookingEventAsync(MakeEvent(BookingEventKind.CANCELLED));

        email.Verify(e => e.SendEmailAsync("jane@example.com", "Booking Cancelled - Fleeman",
            It.Is<string>(b => b.Contains("ABC12345") && b.Contains("has been cancelled"))), Times.Once);
        email.VerifyNoOtherCalls();
    }

    [Test]
    public async Task EmailObserver_NoCustomerEmail_SendsNothing()
    {
        var email = new Mock<IEmailService>();
        var observer = new EmailBookingObserver(email.Object);

        await observer.OnBookingEventAsync(MakeEvent(BookingEventKind.CREATED, email: null));

        email.VerifyNoOtherCalls();
    }

    // ---------- AuditLogBookingObserver ----------

    [TestCase(BookingEventKind.CREATED)]
    [TestCase(BookingEventKind.MODIFIED)]
    [TestCase(BookingEventKind.CANCELLED)]
    public async Task AuditObserver_WritesOneStructuredInformationEntryPerEvent(BookingEventKind kind)
    {
        var logger = new ListLogger<AuditLogBookingObserver>();
        var observer = new AuditLogBookingObserver(logger);

        await observer.OnBookingEventAsync(MakeEvent(kind));

        var entry = logger.Entries.Should().ContainSingle().Subject;
        entry.Level.Should().Be(LogLevel.Information);
        entry.Message.Should().Contain(kind.ToString()).And.Contain("ABC12345");
        entry.Properties["EventKind"].Should().Be(kind);
        entry.Properties["ConfirmationNo"].Should().Be("ABC12345");
        entry.Properties["BookingId"].Should().Be(7L);
        entry.Properties["CustomerId"].Should().Be(1L);
    }
}
