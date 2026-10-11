using FlemanApi.DTO;

namespace FlemanApi.Service;

public interface IMaintenanceService
{
    Task<MaintenanceScheduleDTO> ScheduleAsync(ScheduleMaintenanceRequest request);
    Task<MaintenanceScheduleDTO> GetByIdAsync(long id);
    Task<IReadOnlyList<MaintenanceScheduleDTO>> GetDueAsync();
    Task<IReadOnlyList<MaintenanceScheduleDTO>> GetOverdueAsync();
    Task<MaintenanceScheduleDTO> StartAsync(long id, MaintenanceTransitionRequest request);
    Task<MaintenanceScheduleDTO> CompleteAsync(long id, MaintenanceTransitionRequest request);
    Task<MaintenanceScheduleDTO> CancelAsync(long id, MaintenanceTransitionRequest request);
}
