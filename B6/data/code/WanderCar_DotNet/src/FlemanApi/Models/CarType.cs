using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("car_types")]
public class CarType
{
    [Key]
    public long CarTypeId { get; set; }

    [Required, MaxLength(100)]
    public string CarTypeName { get; set; } = string.Empty;

    [Required]
    public double DailyRate { get; set; }

    public double? WeeklyRate { get; set; }
    public double? MonthlyRate { get; set; }
    public DateOnly? RateValidFrom { get; set; }
    public DateOnly? RateValidTo { get; set; }
    public string? ImagePath { get; set; }
}
