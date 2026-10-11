namespace FlemanApi.DTO;

// The customer reserves a carTypeId, not a specific carId — the actual
// vehicle is assigned by staff at hand-over (see HandoverRequest). No
// add-on prices are sent either, only addonId + quantity; the server looks
// up the current rate and computes every subtotal itself.
public class CreateBookingRequest
{
    public long? CustomerId { get; set; }
    public long? CarTypeId { get; set; }
    public long? PickupHubId { get; set; }
    public long? DropHubId { get; set; }
    public DateTime? PickupDatetime { get; set; }
    public DateTime? ReturnDatetime { get; set; }
    public List<AddonLineRequest>? Addons { get; set; }

    // Sent by the client for display purposes only — the server recomputes
    // and trusts its own total, never this value, when persisting.
    public double? EstimatedAmount { get; set; }

    public string? Remarks { get; set; }
}
