using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

public sealed class InProgressState : MaintenanceStateBase
{
    public static readonly InProgressState Instance = new();

    private InProgressState() { }

    public override MaintenanceStatus Status => MaintenanceStatus.IN_PROGRESS;

    public override void Complete(MaintenanceContext context)
    {
        ReleaseCar(context);
        context.Schedule.CompletedAt = context.Now;
        context.TransitionTo(CompletedState.Instance);
    }

    public override void Cancel(MaintenanceContext context)
    {
        ReleaseCar(context);
        context.TransitionTo(CancelledState.Instance);
    }
}
