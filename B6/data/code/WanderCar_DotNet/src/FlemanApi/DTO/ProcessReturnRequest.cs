using FlemanApi.Models;

namespace FlemanApi.DTO;

public class ProcessReturnRequest
{
    public int? ExtraMiles { get; set; } = 0;
    public double? ExtraChargeAmount { get; set; } = 0.0;
    public string? DamageNotes { get; set; } = "";
    public int? FuelStatus { get; set; }

    // How the customer paid at the return desk. Null means "not collected
    // yet", which the service records as PaymentStatus = PENDING.
    public PaymentType? PaymentType { get; set; }
}
