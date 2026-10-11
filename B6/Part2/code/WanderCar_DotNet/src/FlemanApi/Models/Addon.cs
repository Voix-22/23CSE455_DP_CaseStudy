using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("addons")]
public class Addon
{
    [Key]
    public long AddonId { get; set; }

    [Required, MaxLength(100)]
    public string AddonName { get; set; } = string.Empty;

    [Required]
    public double DailyRate { get; set; }

    public DateOnly? RateValidFrom { get; set; }
    public DateOnly? RateValidTo { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}
