using FlemanApi.Models;

namespace FlemanApi.DTO;

public class ScheduleMaintenanceRequest
{
    public long? CarId { get; set; }
    public MaintenanceType? MaintenanceType { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Notes { get; set; }
}
