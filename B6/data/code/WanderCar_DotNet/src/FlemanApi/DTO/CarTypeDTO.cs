namespace FlemanApi.DTO;

public class CarTypeDTO
{
    public long CarTypeId { get; set; }
    public string CarTypeName { get; set; } = string.Empty;
    public double DailyRate { get; set; }
    public double? WeeklyRate { get; set; }
    public double? MonthlyRate { get; set; }
    public DateOnly? RateValidFrom { get; set; }
    public DateOnly? RateValidTo { get; set; }
    public string? ImagePath { get; set; }

    // Only populated by a hub-scoped call (GetCarTypesForHub) — how many
    // cars of this type are free to reserve at that hub for the requested
    // dates right now. Null in the plain catalog listing. 0 means the
    // category is shown but unbookable, not hidden.
    public int? AvailableCount { get; set; }
}
