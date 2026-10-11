using System.Net;
using FlemanApi.Exceptions;
using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

public sealed class ScheduledState : MaintenanceStateBase
{
    public static readonly ScheduledState Instance = new();

    private ScheduledState() { }

    public override MaintenanceStatus Status => MaintenanceStatus.SCHEDULED;

    // Work can only begin on a car that is physically free right now.
    public override void Start(MaintenanceContext context)
    {
        if (context.Car.Status != CarStatus.AVAILABLE)
        {
            throw new ApiException(
                $"Cannot start maintenance: car {context.Car.CarId} is not available",
                HttpStatusCode.Conflict);
        }

        context.Car.Status = CarStatus.UNDER_MAINTENANCE;
        context.Car.IsAvailable = false;
        context.Schedule.StartedAt = context.Now;
        context.TransitionTo(InProgressState.Instance);
    }

    // Nothing was done to the car yet, so it is left alone.
    public override void Cancel(MaintenanceContext context) =>
        context.TransitionTo(CancelledState.Instance);
}
