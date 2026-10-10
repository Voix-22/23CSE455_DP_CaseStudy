# Change 3 — Fleet-maintenance scheduling (State)

Owner: Samridhi Singh · Branch: `ch03-maintenance-scheduling` (cut from `baseline` = `dfc61ab8`) · Integrated after `ch02`, tagged `ch03`.

## Baseline behaviour

There is no maintenance feature. `CarStatus` has a value `UNDER_MAINTENANCE`. `AvailabilityRepository.CountCapacityAsync` leaves such cars out of capacity, and `CountByCarTypeAsync` counts them for the staff dashboard. `StaffService.HandoverVehicleAsync` refuses a car that is not `AVAILABLE`. The application never sets a car to `UNDER_MAINTENANCE` itself; only seed data does.

## Requirement

Staff can schedule maintenance for a car, then start it, complete it or cancel it. Staff can also list what is due or overdue. The schedule's workflow uses the **State** pattern.

## Design

- Entity `MaintenanceSchedule` (table `maintenance_schedules`) has these columns: `MaintenanceScheduleId`, `CarId` (a plain id with no navigation property), `MaintenanceType` (SERVICE / INSPECTION / REPAIR), `ScheduledDate` (`date`), `Status` (SCHEDULED / IN_PROGRESS / COMPLETED / CANCELLED), `StartedAt`, `CompletedAt` and `Notes`. Both enums are stored as strings.
- State pattern:
  - `IMaintenanceState` declares `Start`, `Complete` and `Cancel`.
  - `MaintenanceStateBase` makes all three throw `ApiException` 409 by default.
  - The concrete states are `ScheduledState`, `InProgressState`, `CompletedState` and `CancelledState`.
  - The context is `MaintenanceContext`. It picks the state from `Status` with a dictionary lookup.
  - `MaintenanceService` has no `switch` or `if` chain on `Status`.
- Service rules:
  - **Schedule:** the car must exist (otherwise 404), and the date must be today or later (otherwise 400).
  - **Start:** allowed only from SCHEDULED, and only when the car is `AVAILABLE` (otherwise 409). It sets the car to `UNDER_MAINTENANCE` with `IsAvailable = false`, and stamps `StartedAt`.
  - **Complete:** allowed only from IN_PROGRESS. It sets the car back to `AVAILABLE` with `IsAvailable = true`, and stamps `CompletedAt`.
  - **Cancel:** allowed from SCHEDULED, where the car is untouched, and from IN_PROGRESS, where the car is released.
  - **Due:** SCHEDULED items dated today. **Overdue:** SCHEDULED items dated before today.
- "Now" comes from an injected .NET 8 `TimeProvider` (`TimeProvider.System` in production). New code never calls `DateTime.Now`.
- REST API: `MaintenanceController` at `api/staff/maintenance` with `[Authorize(Roles = "STAFF")]`.

| Method | Route | Body |
|---|---|---|
| POST | `/api/staff/maintenance` | `ScheduleMaintenanceRequest` |
| GET | `/api/staff/maintenance/due` | – |
| GET | `/api/staff/maintenance/overdue` | – |
| GET | `/api/staff/maintenance/{id}` | – |
| POST | `/api/staff/maintenance/{id}/start` | optional `MaintenanceTransitionRequest` |
| POST | `/api/staff/maintenance/{id}/complete` | optional `MaintenanceTransitionRequest` |
| POST | `/api/staff/maintenance/{id}/cancel` | optional `MaintenanceTransitionRequest` |

## Business values chosen

| Decision | Value |
|---|---|
| "Today" | Server-local calendar day (`TimeProvider.GetLocalNow().Date`). The rest of the app already uses `DateTime.Now`. |
| Scheduling for today | Allowed. Only dates strictly before today are rejected. |
| Time part of `scheduledDate` | Dropped. The column is a `date`. |
| Scheduling a car that is currently BOOKED | Allowed. Only **Start** needs the car to be `AVAILABLE`. |
| Notes | 500 characters at most. Notes sent with start, complete or cancel are appended as `"<old> \| Start: <note>"`. If the combined text would exceed 500 characters, the request is rejected with 400 before any state change. |
| Error codes | 400 for validation and past dates, 404 for an unknown car or schedule, 409 for an illegal transition or a car that is not `AVAILABLE` at Start. |

## Acceptance criteria

1. Starting maintenance removes the car from `CountCapacityAsync` and from the handover picker, with no change to that existing code. Evidence: `MaintenanceFleetAvailabilityTests`.
2. Every illegal transition returns 409 and changes nothing. Evidence: the `*_ThrowsConflictAndChangesNothing` tests in `MaintenanceStateTests` and `MaintenanceServiceTests`.
3. Due and overdue lists are correct on the boundary day with a fixed clock. Evidence: `GetDueAsync_*`, `GetOverdueAsync_*` and `DueAndOverdue_*` in `MaintenanceServiceTests`.
4. All existing tests pass unchanged. Evidence: `test_result.txt`.
5. New tests cover each state's three operations, the service rules and the validator. Evidence: `MaintenanceStateTests`, `MaintenanceServiceTests` and `MaintenanceRequestValidatorsTests`.
