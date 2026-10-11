using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

// Terminal: overrides nothing, so start / complete / cancel all return 409.
public sealed class CancelledState : MaintenanceStateBase
{
    public static readonly CancelledState Instance = new();

    private CancelledState() { }

    public override MaintenanceStatus Status => MaintenanceStatus.CANCELLED;
}
