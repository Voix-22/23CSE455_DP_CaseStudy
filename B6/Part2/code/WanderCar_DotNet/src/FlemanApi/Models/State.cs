using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

[Table("states")]
public class State
{
    [Key]
    public long StateId { get; set; }

    [Required, MaxLength(100)]
    public string StateName { get; set; } = string.Empty;
}
