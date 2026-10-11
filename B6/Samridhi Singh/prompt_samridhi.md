# CH01 prompt - Fleet-maintenance scheduling (State)

Owner: Samridhi Singh. 

**Files to attach from `WanderCar_DotNet/src/`:** `FlemanApi/Models/Car.cs`, `FlemanApi/Data/FlemanDbContext.cs`, `FlemanApi/Repository/*.cs`, `FlemanApi/Service/StaffService.cs`, `FlemanApi/Service/VehicleService.cs`, `FlemanApi/Controllers/StaffController.cs`, `FlemanApi/Validators/AirportRequestValidator.cs`, `FlemanApi/Exceptions/*.cs`, `FlemanApi/Program.cs`, `FlemanApi.Tests/Services/StaffServiceTests.cs`, `FlemanApi.Tests/Repository/AvailabilityRepositoryTests.cs`, `FlemanApi.Tests/TestHelpers/*.cs`

---

You are helping me implement one change in an existing ASP.NET Core 8 application for a university design-patterns case study.

## Project context (same for every change)

- Course: 23CSE455 Design Patterns, team B8. Case study: "Design Pattern Identification and Evolution in an ASP.NET Core-Based Vehicle Rental Application".
- Application: WanderCar / Fleman, repository https://github.com/Naveen-verma-7697/FleetManagment, baseline commit `dfc61ab8dd6f9bbb2301171457dc03946a249576`.
- Only the .NET backend is in scope: `WanderCar_DotNet/src/FlemanApi` (ASP.NET Core 8, EF Core 8 with Pomelo MySQL, AutoMapper, FluentValidation) and its tests in `WanderCar_DotNet/src/FlemanApi.Tests` (NUnit 3, Moq, FluentAssertions, EF Core InMemory). Do not touch the React frontend or the Java backend.
- Architecture: Controller -> Service (interface + one implementation, registered in `Program.cs`) -> `IGenericRepository<TEntity,long>` / `IAvailabilityRepository` -> `FlemanDbContext`. Entities use plain id columns, no navigation properties. Enums are stored as strings (`HasConversion<string>()` in `FlemanDbContext.OnModelCreating`). Errors are thrown as `ApiException(message, HttpStatusCode)` or `ResourceNotFoundException` and turned into JSON by `ExceptionHandlingMiddleware`.
- Tests build services by hand with `MockRepositoryFactory.Create<TEntity,long>(list, keySelector, keySetter)` and `TestMapperFactory.Create()`.
- The baseline has 183 NUnit tests. They are the behaviour-preservation evidence for this assignment.

## Rules you must follow

1. Work only from the source files I give you. Do not invent classes, methods, files or line numbers. If you need a file I have not supplied, ask for it.
2. Follow the existing code style: file-scoped namespaces, constructor injection, async methods ending in `Async`, DTOs in `DTO/`, validators in `Validators/`, one interface per service.
3. Existing public behaviour must not change unless the requirement says so. Do not delete or weaken an existing test. If a constructor signature changes, the only allowed edit to an existing test is passing the new argument in its `SetUp`.
4. Give me complete file contents for every new file and exact before/after snippets for every edited file. Tell me which file each snippet belongs to.
5. Never state that the code compiles or that tests pass. I will run `dotnet build` and `dotnet test` myself and paste the real output back to you if something fails.
6. Register new services in `Program.cs` in one clearly commented block for this change, placed after the existing service registrations, so the four changes merge cleanly.
7. Use the pattern named in the requirement properly, with every role mapped to a real class. After the code, give me a table: pattern role -> class/interface/method -> file.

## Team workflow

- One shared fork of the repository. Tag `baseline` = `dfc61ab8`. Each change is developed on its own branch cut from `baseline`.
- Changes are integrated in order on the `evolution` branch: CH01 vehicle categories -> CH02 pricing policies -> CH03 maintenance scheduling -> CH04 booking notifications. Before your change is merged, rebase your branch on the previous change's tag and re-run all tests.
- Each change ends as one commit on `evolution`, tagged `ch01`, `ch02`, `ch03`, `ch04`. That commit is the "source version after ChangeN".

## Evidence I must hand in (folder `Part2/changes/ChangeN/`)

Help me produce the text for these; the numbers come from my own machine.

| File | Content |
|---|---|
| `requirement.md` | The requirement and acceptance criteria below, plus any business values chosen |
| `before_commit.txt` / `after_commit.txt` | `git rev-parse` of the commit before and after the change on `evolution` |
| `changed_files.txt` | `git diff --name-status <before> <after>` |
| `diff.patch` | `git diff <before> <after> -- WanderCar_DotNet` |
| `test_result.txt` | Full real output of `dotnet test` after the change |
| `pattern_effect.md` | For every baseline pattern instance: preserved / modified / extended / replaced / removed, and any newly introduced instance, each with a reason and participant file:line |
| `refactoring.md` | Real code smell, refactoring technique, the diff hunk, and the test evidence that behaviour did not change. If no existing code was refactored, say so plainly |
| `metrics.csv` | Source LOC and test totals before and after, same counting rule both times |

LOC rule used by the team (Git Bash, run in `WanderCar_DotNet/src/FlemanApi`):
`find . -name '*.cs' -not -path './obj/*' -not -path './bin/*' -not -path './Migrations/*' | xargs cat | grep -vE '^\s*$' | grep -vE '^\s*//' | wc -l`

Also save this exact prompt, the files supplied, and your raw answer: they are logged in `Part2/llm.csv` and a teammate will verify your claims against the code.


## The change: CH03 - Fleet-maintenance scheduling

**Baseline behaviour.** There is no maintenance feature. `CarStatus` has a value `UNDER_MAINTENANCE`; `AvailabilityRepository.CountCapacityAsync` excludes such cars from capacity and `CountByCarTypeAsync` counts them for the staff dashboard; `StaffService.HandoverVehicleAsync` refuses a car that is not `AVAILABLE`. Nothing in the application ever sets a car to `UNDER_MAINTENANCE` except seed data.

**Requirement.** Staff can schedule maintenance for a car, start it, complete it or cancel it, and can list what is due or overdue. Use the **State** pattern for the schedule's workflow.

**Design to implement.**
- Entity `MaintenanceSchedule` (table `maintenance_schedules`): id, `CarId` (plain id, no navigation property), `MaintenanceType` enum (SERVICE, INSPECTION, REPAIR), `ScheduledDate`, `Status` enum (SCHEDULED, IN_PROGRESS, COMPLETED, CANCELLED), `StartedAt`, `CompletedAt`, `Notes`. Enums stored as strings.
- State pattern: `IMaintenanceState` with `Start`, `Complete` and `Cancel` operations; concrete states `ScheduledState`, `InProgressState`, `CompletedState`, `CancelledState`; a context that picks the state object from the entity's `Status`. An operation that is illegal in a state throws `ApiException` with 409 Conflict. There must be no `switch` or `if` chain on `Status` in the service.
- `IMaintenanceService` / `MaintenanceService`:
  - Schedule: the car must exist; the date must not be in the past.
  - Start: allowed only from SCHEDULED and only when the car is `AVAILABLE` (a `BOOKED` car is out with a customer); sets the car to `UNDER_MAINTENANCE` and `IsAvailable = false`.
  - Complete: allowed only from IN_PROGRESS; sets the car back to `AVAILABLE` and `IsAvailable = true`.
  - Cancel: allowed from SCHEDULED; from IN_PROGRESS it also releases the car.
  - Due list: SCHEDULED items whose date is today. Overdue list: SCHEDULED items whose date is before today.
- Inject .NET 8 `TimeProvider` for "now" so due and overdue can be tested with a fixed clock. Do not call `DateTime.Now` in new code.
- `MaintenanceController` at `api/staff/maintenance` with `[Authorize(Roles = "STAFF")]`, request DTOs and a FluentValidation validator.
- `DbSet` in `FlemanDbContext`, enum conversions, and one EF Core migration. Generate the migration only after rebasing on the `ch01` tag, because CH01 also adds a migration and the two would conflict in `FlemanDbContextModelSnapshot.cs`. Tell me the exact `dotnet ef migrations add` command; do not hand-write the snapshot.

**Acceptance criteria.**
1. Starting maintenance removes the car from `CountCapacityAsync` and from the handover picker without any change to that existing code.
2. Every illegal transition (for example Complete from SCHEDULED, Start from COMPLETED) returns 409 and changes nothing.
3. Due and overdue lists are correct on the boundary day with a fixed clock.
4. All existing tests pass unchanged.
5. New tests cover each state's three operations, the service rules above, and the validator.

**Refactoring to document.** This change is additive. If you find a real smell in the code you touch, name it and fix it with evidence; otherwise `refactoring.md` says that no existing code was refactored.

**Pattern effects to expect.** Newly introduced: State, "Maintenance Schedule Workflow". The Generic Entity Repository and Dependency Injection instances are extended with new clients and registrations. The Fleet Availability Query Repository is preserved and now has real data to exclude.
