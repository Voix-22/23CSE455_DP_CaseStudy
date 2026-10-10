# Change 3: Pattern effects

All paths are relative to `WanderCar_DotNet/src/`. Line numbers refer to the CH03 branch before rebasing onto `ch02`, so re-check them after the rebase.

> Only the pattern instances named in the CH03 brief are listed individually below. Any other baseline instance in the team catalogue that CH03 does not touch should be marked **preserved**, with the reason "no file of this instance is changed by CH03 (see `changed_files.txt`)".

## Newly introduced

### State: "Maintenance Schedule Workflow"

The legal transitions and their side effects on the car are spread across one class per status. `MaintenanceService` holds no conditional logic on `Status`; the only references are the query predicates for the due/overdue lists at `MaintenanceService.cs:90` and `:98`.

| Role | Class / method | File:line |
|---|---|---|
| State (interface) | `IMaintenanceState` with `Start`, `Complete`, `Cancel` and `Status` | `FlemanApi/Service/Maintenance/IMaintenanceState.cs:8` |
| Default behaviour (abstract state) | `MaintenanceStateBase`: every operation throws `ApiException` 409 unless a subclass overrides it | `FlemanApi/Service/Maintenance/MaintenanceStateBase.cs:10` |
| ConcreteState | `ScheduledState`, which overrides `Start` (line 16) and `Cancel` (line 30) | `FlemanApi/Service/Maintenance/ScheduledState.cs:7` |
| ConcreteState | `InProgressState`, which overrides `Complete` (line 12) and `Cancel` (line 20) | `FlemanApi/Service/Maintenance/InProgressState.cs:5` |
| ConcreteState (terminal) | `CompletedState`, which overrides nothing | `FlemanApi/Service/Maintenance/CompletedState.cs:6` |
| ConcreteState (terminal) | `CancelledState`, which overrides nothing | `FlemanApi/Service/Maintenance/CancelledState.cs:6` |
| Context | `MaintenanceContext`, which selects the state from the persisted `Status` with a table lookup (line 28) | `FlemanApi/Service/Maintenance/MaintenanceContext.cs:10` |
| Context → State delegation | `MaintenanceContext.Start` / `Complete` / `Cancel` | `FlemanApi/Service/Maintenance/MaintenanceContext.cs:36-38` |
| State change | `MaintenanceContext.TransitionTo`, which keeps the state object and `Schedule.Status` in step | `FlemanApi/Service/Maintenance/MaintenanceContext.cs:42` |
| Client | `MaintenanceService.TransitionAsync` | `FlemanApi/Service/MaintenanceService.cs:118` |
| Persisted state | `MaintenanceSchedule.Status` (`MaintenanceStatus` enum) | `FlemanApi/Models/MaintenanceSchedule.cs` |

Each concrete state is a stateless singleton (`Instance`), so the context shares the state objects rather than allocating new ones.

## Extended

### Generic Entity Repository

- **Effect:** extended. `IGenericRepository<,>` and `GenericRepository<,>` are unchanged. They gain a new entity and two new clients through the existing open-generic registration (`FlemanApi/Program.cs:179`).
- **New entity:** `MaintenanceSchedule`.
- **New client 1:** `MaintenanceService` uses `IGenericRepository<MaintenanceSchedule,long>` (`FlemanApi/Service/MaintenanceService.cs:19`).
- **New client 2:** `MaintenanceService` uses `IGenericRepository<Car,long>` (`FlemanApi/Service/MaintenanceService.cs:20`).
- **Persistence:** the repository resolves its `DbSet` through the new `FlemanDbContext.MaintenanceSchedules` (`FlemanApi/Data/FlemanDbContext.cs:22`).

### Dependency Injection

- **Effect:** extended. These registrations are new, in one CH03 block (`FlemanApi/Program.cs:211-214`):
  - `TimeProvider.System` as a singleton (`:213`)
  - `IMaintenanceService` → `MaintenanceService` as scoped (`:214`)
- **New constructor injection:** `MaintenanceController(IMaintenanceService)` (`FlemanApi/Controllers/MaintenanceController.cs:15`) and `MaintenanceService(..., TimeProvider)` (`FlemanApi/Service/MaintenanceService.cs:24-26`).
- **Validators:** `ScheduleMaintenanceRequestValidator` and `MaintenanceTransitionRequestValidator` are picked up by the existing `AddValidatorsFromAssembly` scan, with no new registration.

## Preserved

### Fleet Availability Query Repository

- **Effect:** preserved. `IAvailabilityRepository` and `AvailabilityRepository` are not edited.
- **What changes:** these queries now see real data. `ScheduledState.Start` sets `Car.Status = UNDER_MAINTENANCE`, which the existing queries already handle:
  - `CountCapacityAsync` excludes such cars (`FlemanApi/Repository/AvailabilityRepository.cs:20`).
  - `CountByCarTypeAsync` counts them (`FlemanApi/Repository/AvailabilityRepository.cs:44`).
- **Handover:** the handover picker `StaffService.GetAvailableCarsForHandoverAsync` (`FlemanApi/Service/StaffService.cs:227`) and the handover guard (`FlemanApi/Service/StaffService.cs:82`) also exclude the car with no edit.
- **Evidence:** `FlemanApi.Tests/Services/MaintenanceFleetAvailabilityTests.cs` runs the unchanged `AvailabilityRepository` and `StaffService` against the same InMemory database that `MaintenanceService` writes to.

### Global exception handling (middleware)

- **Effect:** preserved. `ExceptionHandlingMiddleware` is not edited.
- **New client:** the 409 `ApiException`s thrown by `MaintenanceStateBase` reach the client as the existing `{ message, status, timestamp }` JSON, with no new handling.
