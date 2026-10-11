using FlemanApi.DTO;

namespace FlemanApi.Service;

// Mirrors com.fleman.service.impl.EmailService — implemented in full
// (MailKit + background queue) alongside StaffService/InvoicePdfService.
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);

    Task SendBookingInvoiceAsync(
        string to, string customerName, BookingResponseDTO booking, InvoiceResponseDTO invoice, byte[] pdf);
}
