namespace FlemanApi.DTO;

// The booking only reserved a category, so staff must pick which specific
// car of that type (at the pickup hub) actually goes to the customer.
// StaffService.HandoverVehicle validates it's the right type, the right
// hub, and still AVAILABLE before assigning it.
public class HandoverRequest
{
    public long? CarId { get; set; }
    public int? FuelStatus { get; set; }
    public string? Notes { get; set; }
}
