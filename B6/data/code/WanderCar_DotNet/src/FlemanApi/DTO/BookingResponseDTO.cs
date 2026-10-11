namespace FlemanApi.DTO;

// The shape every booking-related endpoint returns: the raw booking fields
// plus car/carType/customer/pickupHub/dropHub/addonLines all nested in, so
// the UI never has to make four follow-up requests.
public class BookingResponseDTO
{
    public long BookingId { get; set; }
    public string ConfirmationNo { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public long CustomerId { get; set; }
    public long? CarId { get; set; }
    public long PickupHubId { get; set; }
    public long DropHubId { get; set; }
    public DateTime PickupDatetime { get; set; }
    public DateTime ReturnDatetime { get; set; }
    public DateTime? ActualHandoverDatetime { get; set; }
    public DateTime? ActualReturnDatetime { get; set; }
    public int? HandoverFuelLevel { get; set; }
    public string? BookingStatus { get; set; }
    public double? EstimatedAmount { get; set; }
    public string? Remarks { get; set; }

    public CarDTO? Car { get; set; }
    public CarTypeDTO? CarType { get; set; }
    public CustomerDTO? Customer { get; set; }
    public HubDTO? PickupHub { get; set; }
    public HubDTO? DropHub { get; set; }
    public List<BookingDetailDTO>? AddonLines { get; set; }
}
