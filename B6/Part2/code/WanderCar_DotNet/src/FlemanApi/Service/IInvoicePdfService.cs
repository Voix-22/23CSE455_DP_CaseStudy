namespace FlemanApi.Service;

public interface IInvoicePdfService
{
    Task<byte[]> GenerateInvoiceAsync(long invoiceId);
}
