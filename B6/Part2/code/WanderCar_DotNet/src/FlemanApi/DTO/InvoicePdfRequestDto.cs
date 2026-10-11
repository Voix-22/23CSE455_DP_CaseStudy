namespace FlemanApi.DTO;

// Self-contained payload sent to the Java backend's POST /api/invoices/pdf
// (see JavaInvoicePdfService) — the Java service has no DB access for this
// call, so every field it needs to render the PDF travels in the request.
public class InvoicePdfRequestDto
{
    public string? CustomerName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? ConfirmationNo { get; set; }
    public string? VehicleNumber { get; set; }
    public DateTime? HandoverDatetime { get; set; }
    public DateTime? ReturnDatetime { get; set; }
    public int? Days { get; set; }
    public double? RentalAmount { get; set; }
    public double? AddonAmount { get; set; }
    public double? FuelCharge { get; set; }
    public int? HandoverFuelLevel { get; set; }
    public int? ReturnFuelLevel { get; set; }
    public double? ExtraChargeAmount { get; set; }
    public int? ExtraMiles { get; set; }
    public string? DamageNotes { get; set; }
    public double? TotalAmount { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentStatus { get; set; }
    public string? PaymentReference { get; set; }
    public List<InvoiceLineItemDto> Lines { get; set; } = new();
}

public class InvoiceLineItemDto
{
    public string? AddonName { get; set; }
    public int? Quantity { get; set; }
    public double? AddonRate { get; set; }
    public double? Subtotal { get; set; }
}
