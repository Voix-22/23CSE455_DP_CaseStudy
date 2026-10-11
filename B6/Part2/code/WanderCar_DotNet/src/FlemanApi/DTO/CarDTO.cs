namespace FlemanApi.DTO;

public class CarDTO
{
    public long CarId { get; set; }
    public long CarTypeId { get; set; }
    public long HubId { get; set; }
    public string VehicleNumber { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public int? ManufactureYear { get; set; }
    public string? Color { get; set; }
    public string? FuelType { get; set; }
    public int? SeatingCapacity { get; set; }
    public double? Mileage { get; set; }
    public int? Odometer { get; set; }
    public int? FuelLevel { get; set; }
    public bool IsAvailable { get; set; }
    public string? Status { get; set; }

    // Distinct from Status/IsAvailable (the car's persisted, not-date-
    // specific state): true only when this car has no CONFIRMED/ONGOING
    // booking overlapping the requested pickup/return window.
    public bool BookableNow { get; set; }
    public CarTypeDTO? CarType { get; set; }
}
