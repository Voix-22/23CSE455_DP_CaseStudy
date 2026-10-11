namespace FlemanApi.DTO;

// One row of the staff dashboard: how many cars of this type exist (at the
// hub in question, or fleet-wide if no hub was specified), split across
// available / handed-over / in-maintenance right now.
public class CarTypeAvailabilityDTO
{
    public long CarTypeId { get; set; }
    public string CarTypeName { get; set; } = string.Empty;
    public long TotalCars { get; set; }
    public long AvailableCars { get; set; }
    public long BookedCars { get; set; }
    public long MaintenanceCars { get; set; }
}
