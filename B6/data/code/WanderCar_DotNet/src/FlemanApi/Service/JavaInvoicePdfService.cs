using FlemanApi.DTO;
using FlemanApi.Models;
using FlemanApi.Repository;

namespace FlemanApi.Service;

// Generates the invoice PDF via the Java backend (fleman-backend, see
// InvoiceController.generateInvoicePdf) instead of generating it locally
// with QuestPDF. The two backends run against separate databases with
// independently auto-incrementing ids, so this fetches the invoice/booking/
// line data from *this* app's own database first and sends the full
// payload — the Java side never looks anything up by id itself. Requires
// the Java backend to be running on JavaMicroservice:BaseUrl — see
// JavaMicroserviceClient. Swap the DI registration in Program.cs back to
// InvoicePdfService (QuestPDF) to generate locally instead.
public class JavaInvoicePdfService : IInvoicePdfService
{
    private readonly IGenericRepository<InvoiceHeader, long> _invoiceHeaders;
    private readonly IGenericRepository<InvoiceDetail, long> _invoiceDetails;
    private readonly IGenericRepository<BookingHeader, long> _bookingHeaders;
    private readonly IJavaMicroserviceClient _javaClient;

    public JavaInvoicePdfService(
        IGenericRepository<InvoiceHeader, long> invoiceHeaders,
        IGenericRepository<InvoiceDetail, long> invoiceDetails,
        IGenericRepository<BookingHeader, long> bookingHeaders,
        IJavaMicroserviceClient javaClient)
    {
        _invoiceHeaders = invoiceHeaders;
        _invoiceDetails = invoiceDetails;
        _bookingHeaders = bookingHeaders;
        _javaClient = javaClient;
    }

    public async Task<byte[]> GenerateInvoiceAsync(long invoiceId)
    {
        var invoice = await _invoiceHeaders.GetByIdAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found");
        var booking = await _bookingHeaders.GetByIdAsync(invoice.BookingId)
            ?? throw new InvalidOperationException("Booking not found");
        var lines = await _invoiceDetails.FindAsync(d => d.InvoiceId == invoiceId);

        var request = new InvoicePdfRequestDto
        {
            CustomerName = invoice.CustomerName,
            Email = invoice.Email,
            Phone = invoice.Phone,
            ConfirmationNo = booking.ConfirmationNo,
            VehicleNumber = invoice.VehicleNumber,
            HandoverDatetime = invoice.HandoverDatetime,
            ReturnDatetime = invoice.ReturnDatetime,
            Days = invoice.Days,
            RentalAmount = invoice.RentalAmount,
            AddonAmount = invoice.AddonAmount,
            FuelCharge = invoice.FuelCharge,
            HandoverFuelLevel = invoice.HandoverFuelLevel,
            ReturnFuelLevel = invoice.ReturnFuelLevel,
            ExtraChargeAmount = invoice.ExtraChargeAmount,
            ExtraMiles = invoice.ExtraMiles,
            DamageNotes = invoice.DamageNotes,
            TotalAmount = invoice.TotalAmount,
            PaymentType = invoice.PaymentType?.ToString(),
            PaymentStatus = invoice.PaymentStatus?.ToString(),
            PaymentReference = invoice.PaymentReference,
            Lines = lines.Select(l => new InvoiceLineItemDto
            {
                AddonName = l.AddonName,
                Quantity = l.Quantity,
                AddonRate = l.AddonRate,
                Subtotal = l.Subtotal,
            }).ToList(),
        };

        return await _javaClient.GenerateInvoicePdfAsync(request);
    }
}
