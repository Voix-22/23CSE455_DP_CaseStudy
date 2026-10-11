using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

public enum FuelType { PETROL, DIESEL, CNG, ELECTRIC, HYBRID }
public enum CarStatus { AVAILABLE, BOOKED, UNDER_MAINTENANCE }

[Table("cars")]
public class Car
{
    [Key]
    public long CarId { get; set; }

    [Required]
    public long CarTypeId { get; set; }

    [Required]
    public long HubId { get; set; }

    [Required, MaxLength(20)]
    public string VehicleNumber { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Brand { get; set; }

    [MaxLength(50)]
    public string? Model { get; set; }

    public int? ManufactureYear { get; set; }

    [MaxLength(30)]
    public string? Color { get; set; }

    public FuelType? FuelType { get; set; }

    public int? SeatingCapacity { get; set; }
    public double? Mileage { get; set; }
    public int? Odometer { get; set; }
    public int? FuelLevel { get; set; }
    public bool IsAvailable { get; set; }
    public CarStatus? Status { get; set; }
}
