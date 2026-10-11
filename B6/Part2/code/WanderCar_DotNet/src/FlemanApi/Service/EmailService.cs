using FlemanApi.DTO;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.EmailService — SendEmailAsync/
// SendBookingInvoiceAsync enqueue onto the background worker instead of
// awaiting the SMTP round-trip inline, the .NET equivalent of @Async.
public class EmailService : IEmailService
{
    private readonly IEmailBackgroundQueue _queue;

    public EmailService(IEmailBackgroundQueue queue)
    {
        _queue = queue;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _queue.Enqueue((sender, ct) => sender.SendPlainAsync(to, subject, body));
        return Task.CompletedTask;
    }

    public Task SendBookingInvoiceAsync(
        string to, string customerName, BookingResponseDTO booking, InvoiceResponseDTO invoice, byte[] pdf)
    {
        var paid = string.Equals(invoice.PaymentStatus, "PAID", StringComparison.OrdinalIgnoreCase);
        var subject = $"WanderCar Booking {booking.ConfirmationNo} — Vehicle Returned";
        var body = $"""
            Dear {customerName},

            Your vehicle has been returned and this booking is now complete.

            Booking Number    : {booking.ConfirmationNo}
            Vehicle           : {invoice.VehicleNumber}
            Handed over       : {invoice.HandoverDatetime}
            Returned          : {invoice.ReturnDatetime}
            Duration          : {invoice.Days} day(s)

            Rental Amount     : ₹{invoice.RentalAmount:F2}
            Add-on Amount     : ₹{invoice.AddonAmount:F2}
            Fuel Charge       : ₹{invoice.FuelCharge ?? 0.0:F2}
            Extra Charges     : ₹{invoice.ExtraChargeAmount ?? 0.0:F2}
            Total Amount      : ₹{invoice.TotalAmount:F2}
            Payment Status    : {invoice.PaymentStatus}{(invoice.PaymentReference is not null ? $" ({invoice.PaymentReference})" : "")}

            {(paid ? "Payment has been received in full." : "Payment is still pending collection at this time.")}

            Thank you for choosing WanderCar.

            Invoice attached.

            """;

        _queue.Enqueue((sender, ct) => sender.SendWithAttachmentAsync(to, subject, body, "Invoice.pdf", pdf));
        return Task.CompletedTask;
    }
}
