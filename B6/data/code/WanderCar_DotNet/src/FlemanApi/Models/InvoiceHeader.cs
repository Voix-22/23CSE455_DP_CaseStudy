using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

public enum PaymentType { CASH, UPI, CARD, NET_BANKING }
public enum PaymentStatus { PENDING, PAID, FAILED }

// Reference only, no FK — every cross-table column in this project follows
// the same pattern. customerName/vehicleNumber/hub names are snapshots, not
// live references: a financial record shouldn't drift if the source data
// is edited later.
[Table("invoice_headers")]
public class InvoiceHeader
{
    [Key]
    public long InvoiceId { get; set; }

    [Required]
    public DateTime InvoiceDate { get; set; }

    [Required]
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

    public PaymentType? PaymentType { get; set; }
    public string? PaymentReference { get; set; }
    public PaymentStatus? PaymentStatus { get; set; }

    public int? ExtraMiles { get; set; }
    public double? ExtraChargeAmount { get; set; }

    [MaxLength(1000)]
    public string? DamageNotes { get; set; }

    public int? Days { get; set; }
}
