using FlemanApi.Models;

namespace FlemanApi.DTO;

public class MaintenanceScheduleDTO
{
    public long MaintenanceScheduleId { get; set; }
    public long CarId { get; set; }
    public MaintenanceType MaintenanceType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public MaintenanceStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}
