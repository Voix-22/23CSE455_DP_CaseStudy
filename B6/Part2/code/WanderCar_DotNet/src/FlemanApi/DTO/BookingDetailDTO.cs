namespace FlemanApi.DTO;

public class BookingDetailDTO
{
    public long BookingDetailId { get; set; }
    public long BookingId { get; set; }
    public long AddonId { get; set; }
    public double? AddonRate { get; set; }
    public int? Quantity { get; set; }
    public double? Subtotal { get; set; }
    public AddonDTO? Addon { get; set; }
}
