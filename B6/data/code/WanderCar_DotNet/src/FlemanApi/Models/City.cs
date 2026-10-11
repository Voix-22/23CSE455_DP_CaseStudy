using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("cities")]
public class City
{
    [Key]
    public long CityId { get; set; }

    [Required, MaxLength(100)]
    public string CityName { get; set; } = string.Empty;

    // Logical reference only — no FK constraint, matching the Java entity design.
    [Required]
    public long StateId { get; set; }
}
