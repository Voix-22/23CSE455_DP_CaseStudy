//using FlemanApi.Models;
//using FlemanApi.Repository;
//using QuestPDF.Fluent;
//using QuestPDF.Helpers;
//using QuestPDF.Infrastructure;

//namespace FlemanApi.Service;

//// Mirrors com.fleman.service.impl.InvoicePdfServiceImpl (OpenPDF) using
//// QuestPDF instead — same sections/content, different PDF library.
//public class InvoicePdfService : IInvoicePdfService
//{
//    private readonly IGenericRepository<InvoiceHeader, long> _invoiceHeaders;
//    private readonly IGenericRepository<InvoiceDetail, long> _invoiceDetails;
//    private readonly IGenericRepository<BookingHeader, long> _bookingHeaders;

//    public InvoicePdfService(
//        IGenericRepository<InvoiceHeader, long> invoiceHeaders,
//        IGenericRepository<InvoiceDetail, long> invoiceDetails,
//        IGenericRepository<BookingHeader, long> bookingHeaders)
//    {
//        _invoiceHeaders = invoiceHeaders;
//        _invoiceDetails = invoiceDetails;
//        _bookingHeaders = bookingHeaders;
//    }

//    public async Task<byte[]> GenerateInvoiceAsync(long invoiceId)
//    {
//        var invoice = await _invoiceHeaders.GetByIdAsync(invoiceId)
//            ?? throw new InvalidOperationException("Invoice not found");
//        var booking = await _bookingHeaders.GetByIdAsync(invoice.BookingId)
//            ?? throw new InvalidOperationException("Booking not found");
//        var lines = await _invoiceDetails.FindAsync(d => d.InvoiceId == invoiceId);

//        var document = Document.Create(container =>
//        {
//            container.Page(page =>
//            {
//                page.Size(PageSizes.A4);
//                page.Margin(2, Unit.Centimetre);
//                page.DefaultTextStyle(x => x.FontSize(11));

//                page.Content().Column(col =>
//                {
//                    col.Item().AlignCenter().Text("WanderCar Rental Services").FontSize(20).Bold();
//                    col.Item().AlignCenter().Text("RETURN INVOICE").FontSize(13).Bold();
//                    col.Item().PaddingVertical(5).LineHorizontal(1);

//                    col.Item().Text("Customer Details").FontSize(13).Bold();
//                    col.Item().Text($"Name              : {invoice.CustomerName}");
//                    col.Item().Text($"Email             : {invoice.Email}");
//                    col.Item().Text($"Phone             : {invoice.Phone}");

//                    col.Item().PaddingTop(8).Text("Booking Details").FontSize(13).Bold();
//                    col.Item().Text($"Confirmation No   : {booking.ConfirmationNo}");
//                    col.Item().Text($"Vehicle           : {invoice.VehicleNumber}");
//                    col.Item().Text($"Handed over       : {invoice.HandoverDatetime}");
//                    col.Item().Text($"Returned          : {invoice.ReturnDatetime}");
//                    col.Item().Text($"Duration          : {invoice.Days}{(invoice.Days == 1 ? " day" : " days")}");

//                    col.Item().PaddingVertical(5).LineHorizontal(1);

//                    col.Item().Text("Charges").FontSize(13).Bold();
//                    col.Item().Text($"Rental Amount     : ₹ {invoice.RentalAmount:F2}");
//                    foreach (var line in lines)
//                    {
//                        col.Item().Text($"  {line.AddonName,-16}: {line.Quantity} x ₹ {line.AddonRate:F2} = ₹ {line.Subtotal:F2}");
//                    }
//                    col.Item().Text($"Add-on Amount     : ₹ {invoice.AddonAmount:F2}");

//                    if (invoice.FuelCharge is > 0)
//                    {
//                        col.Item().Text(
//                            $"Fuel Charge       : ₹ {invoice.FuelCharge:F2} (returned at {invoice.ReturnFuelLevel}%, handed over at {invoice.HandoverFuelLevel}%)");
//                    }
//                    if (invoice.ExtraChargeAmount is > 0)
//                    {
//                        var kmNote = invoice.ExtraMiles is > 0 ? $" ({invoice.ExtraMiles} extra km)" : "";
//                        col.Item().Text($"Extra Charges     : ₹ {invoice.ExtraChargeAmount:F2}{kmNote}");
//                    }
//                    if (!string.IsNullOrWhiteSpace(invoice.DamageNotes))
//                    {
//                        col.Item().Text($"Damage Notes      : {invoice.DamageNotes}");
//                    }

//                    col.Item().PaddingVertical(5).LineHorizontal(1);

//                    col.Item().Text("Payment Summary").FontSize(13).Bold();
//                    col.Item().Text($"Total Amount      : ₹ {invoice.TotalAmount:F2}");
//                    col.Item().Text($"Payment Type      : {(invoice.PaymentType is not null ? invoice.PaymentType.ToString() : "Not collected")}");
//                    col.Item().Text($"Payment Status    : {invoice.PaymentStatus}");
//                    col.Item().Text($"Payment Reference : {invoice.PaymentReference}");

//                    col.Item().PaddingVertical(5).LineHorizontal(1);
//                    col.Item().AlignCenter().Text("Thank you for choosing WanderCar.\nWe wish you a safe and pleasant journey!")
//                        .FontSize(13).Bold();
//                });
//            });
//        });

//        return document.GeneratePdf();
//    }
//}
