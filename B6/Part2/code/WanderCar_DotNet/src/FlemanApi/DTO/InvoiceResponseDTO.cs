namespace FlemanApi.DTO;

public class InvoiceResponseDTO
{
    public long InvoiceId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public long BookingId { get; set; }
    public string? CustomerName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? VehicleNumber { get; set; }
    public long? PickupHub { get; set; }
    public long? DropHub { get; set; }
    public DateTime? HandoverDatetime { get; set; }
    public DateTime? ReturnDatetime { get; set; }
    public int? HandoverFuelLevel { get; set; }
    public int? ReturnFuelLevel { get; set; }
    public double? FuelCharge { get; set; }
    public double? RentalAmount { get; set; }
    public double? AddonAmount { get; set; }
    public double? TotalAmount { get; set; }
    public string? PaymentType { get; set; }
    public string? PaymentReference { get; set; }
    public string? PaymentStatus { get; set; }
    public int? ExtraMiles { get; set; }
    public double? ExtraChargeAmount { get; set; }
    public string? DamageNotes { get; set; }
    public int? Days { get; set; }
}
