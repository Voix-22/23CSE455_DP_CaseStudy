using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

public enum BookingStatus { PENDING, CONFIRMED, ONGOING, COMPLETED, CANCELLED }

[Table("booking_headers")]
public class BookingHeader
{
    [Key]
    public long BookingId { get; set; }

    [Required]
    public string ConfirmationNo { get; set; } = string.Empty;

    [Required]
    public DateTime BookingDate { get; set; }

    [Required]
    public long CustomerId { get; set; }

    // Category reserved at booking time — the actual vehicle isn't picked
    // until staff hand-over (see CarId below).
    [Required]
    public long CarTypeId { get; set; }

    // Null until a staff member hands over a specific car at pickup.
    public long? CarId { get; set; }

    [Required]
    public long PickupHubId { get; set; }

    [Required]
    public long DropHubId { get; set; }

    [Required]
    public DateTime PickupDatetime { get; set; }

    [Required]
    public DateTime ReturnDatetime { get; set; }

    // Actual hand-over/return times set by StaffService — the return
    // invoice bills against these, not the planned pickup/return above.
    public DateTime? ActualHandoverDatetime { get; set; }
    public DateTime? ActualReturnDatetime { get; set; }

    // Fuel level (25/50/75/100) snapshotted at hand-over, since Car.FuelLevel
    // gets overwritten again at return.
    public int? HandoverFuelLevel { get; set; }

    public BookingStatus? BookingStatus { get; set; }
    public double? EstimatedAmount { get; set; }

    [MaxLength(255)]
    public string? Remarks { get; set; }
}
