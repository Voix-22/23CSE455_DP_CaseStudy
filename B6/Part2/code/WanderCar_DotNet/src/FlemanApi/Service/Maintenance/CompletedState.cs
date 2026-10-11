using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

// Terminal: overrides nothing, so start / complete / cancel all return 409.
public sealed class CompletedState : MaintenanceStateBase
{
    public static readonly CompletedState Instance = new();

    private CompletedState() { }

    public override MaintenanceStatus Status => MaintenanceStatus.COMPLETED;
}
