using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("booking_details")]
public class BookingDetail
{
    [Key]
    public long BookingDetailId { get; set; }

    [Required]
    public long BookingId { get; set; }

    [Required]
    public long AddonId { get; set; }

    public double? AddonRate { get; set; }
    public int? Quantity { get; set; }
    public double? Subtotal { get; set; }
}
