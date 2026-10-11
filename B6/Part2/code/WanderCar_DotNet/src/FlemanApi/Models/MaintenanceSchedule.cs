using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FlemanApi.Models;

public enum MaintenanceType { SERVICE, INSPECTION, REPAIR }
public enum MaintenanceStatus { SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED }

// CH03 - one planned maintenance job for one car. Status is the persisted
// state of the State pattern in Service/Maintenance.
[Table("maintenance_schedules")]
public class MaintenanceSchedule
{
    [Key]
    public long MaintenanceScheduleId { get; set; }

    // A plain id, like BookingHeader.CarId - no navigation property.
    [Required]
    public long CarId { get; set; }

    public MaintenanceType MaintenanceType { get; set; }

    [Column(TypeName = "date")]
    public DateTime ScheduledDate { get; set; }

    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.SCHEDULED;

    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
