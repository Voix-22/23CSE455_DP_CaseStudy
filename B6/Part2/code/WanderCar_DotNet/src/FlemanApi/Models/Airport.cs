using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("airports")]
public class Airport
{
    [Key]
    public long AirportId { get; set; }

    [Required, MaxLength(10)]
    public string AirportCode { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string AirportName { get; set; } = string.Empty;

    [Required]
    public long CityId { get; set; }

    [Required]
    public long StateId { get; set; }

    [Required]
    public long HubId { get; set; }
}
