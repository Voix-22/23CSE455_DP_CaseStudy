namespace FlemanApi.DTO;

// A partial patch — any null field is left unchanged. Only allowed while
// the booking hasn't been handed over yet (BookingService.ModifyBooking
// enforces this).
public class ModifyBookingRequest
{
    public long? PickupHubId { get; set; }
    public long? DropHubId { get; set; }
    public DateTime? PickupDatetime { get; set; }
    public DateTime? ReturnDatetime { get; set; }
    public long? CarTypeId { get; set; }

    // Null means "leave the add-ons as they are"; an empty list means "the
    // customer removed every add-on" — ModifyBooking distinguishes the two.
    public List<AddonLineRequest>? Addons { get; set; }

    public string? Remarks { get; set; }
}
