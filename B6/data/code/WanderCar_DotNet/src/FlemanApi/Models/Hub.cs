using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("hubs")]
public class Hub
{
    [Key]
    public long HubId { get; set; }

    [Required, MaxLength(100)]
    public string HubName { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Address { get; set; }

    [Required]
    public long CityId { get; set; }

    [Required]
    public long StateId { get; set; }

    [Required, MaxLength(10)]
    public string Pincode { get; set; } = string.Empty;

    [Required, MaxLength(20)]
    public string ContactNo { get; set; } = string.Empty;

    public string? Email { get; set; }
}
