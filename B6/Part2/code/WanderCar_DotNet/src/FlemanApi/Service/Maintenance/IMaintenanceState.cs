using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

// State role of the State pattern: one object per MaintenanceStatus. Each
// operation either performs the transition on the context or refuses it.
public interface IMaintenanceState
{
    MaintenanceStatus Status { get; }

    void Start(MaintenanceContext context);
    void Complete(MaintenanceContext context);
    void Cancel(MaintenanceContext context);
}
