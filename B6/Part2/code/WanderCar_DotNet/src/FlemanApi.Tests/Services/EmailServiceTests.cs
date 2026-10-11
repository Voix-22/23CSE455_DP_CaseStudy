using FlemanApi.Service;
using FluentAssertions;
using Moq;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class EmailServiceTests
{
    private Mock<IEmailBackgroundQueue> _queueMock = null!;
    private EmailService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _queueMock = new Mock<IEmailBackgroundQueue>();
        _service = new EmailService(_queueMock.Object);
    }

    [Test]
    public async Task SendEmailAsync_EnqueuesWorkItem_DoesNotSendSynchronously()
    {
        await _service.SendEmailAsync("to@example.com", "Subject", "Body");

        _queueMock.Verify(
            q => q.Enqueue(It.IsAny<Func<IEmailSender, CancellationToken, Task>>()), Times.Once);
    }

    [Test]
    public async Task SendEmailAsync_EnqueuedWorkItem_CallsSendPlainAsyncOnSender()
    {
        Func<IEmailSender, CancellationToken, Task>? captured = null;
        _queueMock.Setup(q => q.Enqueue(It.IsAny<Func<IEmailSender, CancellationToken, Task>>()))
            .Callback<Func<IEmailSender, CancellationToken, Task>>(item => captured = item);

        await _service.SendEmailAsync("to@example.com", "Subject", "Body");

        captured.Should().NotBeNull();
        var senderMock = new Mock<IEmailSender>();
        await captured!(senderMock.Object, CancellationToken.None);

        senderMock.Verify(s => s.SendPlainAsync("to@example.com", "Subject", "Body"), Times.Once);
    }

    [Test]
    public async Task SendBookingInvoiceAsync_EnqueuesWorkItem_DoesNotSendSynchronously()
    {
        var booking = new FlemanApi.DTO.BookingResponseDTO { ConfirmationNo = "ABC12345" };
        var invoice = new FlemanApi.DTO.InvoiceResponseDTO { PaymentStatus = "PAID" };

        await _service.SendBookingInvoiceAsync("to@example.com", "John Doe", booking, invoice, new byte[] { 1, 2, 3 });

        _queueMock.Verify(
            q => q.Enqueue(It.IsAny<Func<IEmailSender, CancellationToken, Task>>()), Times.Once);
    }
}
