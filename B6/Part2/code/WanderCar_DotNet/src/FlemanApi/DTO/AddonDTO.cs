namespace FlemanApi.DTO;

public class AddonDTO
{
    public long AddonId { get; set; }
    public string AddonName { get; set; } = string.Empty;
    public double DailyRate { get; set; }
    public DateOnly? RateValidFrom { get; set; }
    public DateOnly? RateValidTo { get; set; }
    public string? Description { get; set; }
}
