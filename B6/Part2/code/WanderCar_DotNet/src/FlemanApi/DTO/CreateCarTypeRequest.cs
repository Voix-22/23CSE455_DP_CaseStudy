using FlemanApi.Models;

namespace FlemanApi.DTO;

public class CreateCarTypeRequest
{
    public string CarTypeName { get; set; } = string.Empty;
    public VehicleCategory? Category { get; set; }
    public double? DailyRate { get; set; }
    public double? WeeklyRate { get; set; }
    public double? MonthlyRate { get; set; }
    public string? ImagePath { get; set; }
}
