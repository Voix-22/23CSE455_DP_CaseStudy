using System.Net;
using FlemanApi.Exceptions;
using FlemanApi.Models;

namespace FlemanApi.Service.Maintenance;

// Default behaviour: every operation is illegal (409) until a concrete
// state overrides it, so a state only lists the transitions it allows.
public abstract class MaintenanceStateBase : IMaintenanceState
{
    public abstract MaintenanceStatus Status { get; }

    public virtual void Start(MaintenanceContext context) => throw Illegal("start");
    public virtual void Complete(MaintenanceContext context) => throw Illegal("complete");
    public virtual void Cancel(MaintenanceContext context) => throw Illegal("cancel");

    protected ApiException Illegal(string operation) =>
        new($"Cannot {operation} maintenance that is {Status}", HttpStatusCode.Conflict);

    // Shared by the two transitions that hand the car back to the fleet.
    protected static void ReleaseCar(MaintenanceContext context)
    {
        context.Car.Status = CarStatus.AVAILABLE;
        context.Car.IsAvailable = true;
    }
}
