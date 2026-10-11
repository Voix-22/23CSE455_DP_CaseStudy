using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

// Context role of the State pattern: holds the schedule, the car it is for
// and the current state object, and forwards every operation to that state.
public sealed class MaintenanceContext
{
    private static readonly IReadOnlyDictionary<MaintenanceStatus, IMaintenanceState> States =
        new Dictionary<MaintenanceStatus, IMaintenanceState>
        {
            [MaintenanceStatus.SCHEDULED] = ScheduledState.Instance,
            [MaintenanceStatus.IN_PROGRESS] = InProgressState.Instance,
            [MaintenanceStatus.COMPLETED] = CompletedState.Instance,
            [MaintenanceStatus.CANCELLED] = CancelledState.Instance,
        };

    public MaintenanceSchedule Schedule { get; }
    public Car Car { get; }
    public DateTime Now { get; }
    public IMaintenanceState State { get; private set; }

    public MaintenanceContext(MaintenanceSchedule schedule, Car car, DateTime now)
    {
        Schedule = schedule;
        Car = car;
        Now = now;
        State = States[schedule.Status];
    }

    public void Start() => State.Start(this);
    public void Complete() => State.Complete(this);
    public void Cancel() => State.Cancel(this);

    // Called by the states only: keeps the state object and the persisted
    // Schedule.Status in step.
    public void TransitionTo(IMaintenanceState next)
    {
        State = next;
        Schedule.Status = next.Status;
    }
}
