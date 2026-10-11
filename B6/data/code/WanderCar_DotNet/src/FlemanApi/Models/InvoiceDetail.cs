using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("invoice_details")]
public class InvoiceDetail
{
    [Key]
    public long InvoiceDetailId { get; set; }

    [Required]
    public long InvoiceId { get; set; }

    public string? AddonName { get; set; }
    public int? Quantity { get; set; }
    public double? AddonRate { get; set; }
    public double? Subtotal { get; set; }
}
