# Report text for Part 2 - record CH02 (Bhavana P H)

## 3 Part 2 Evolution and refactoring - CH02: Add new vehicle categories

**Change and acceptance criteria.**
At the baseline a "category" was only a row in `car_types` (Economy, Compact, Sedan, SUV, Luxury, Minivan) with a name and three rates; the code had no category concept, no category-specific rule and no endpoint to create a car type. CH02 gives every car type a vehicle category: STANDARD (all existing car types), and the new ELECTRIC and COMMERCIAL. Each category has its own rules, chosen by the team for the case study: security deposit 0 / 2 x daily rate / 5 x daily rate, and COMMERCIAL additionally requires a weekly rate. Staff can create a car type in any category and anyone can list car types by category.

| # | Acceptance criterion | Observed result |
|---|---|---|
| 1 | Existing car types keep working and are reported as STANDARD with deposit 0 | Met. A database seeded by the baseline build was upgraded by the CH02 build; all six existing types return `"category":"STANDARD","securityDeposit":0` |
| 2 | `POST /api/staff/car-types` (STAFF only) creates a car type in any category and stores that category's deposit | Met. ELECTRIC at 60/day -> deposit 120; COMMERCIAL at 80/day -> 400; STANDARD -> 0. No token -> 401; customer token -> 403 |
| 3 | COMMERCIAL without weekly rate -> 400; duplicate name -> 409 | Met. 400 "weeklyRate is required for COMMERCIAL car types"; 409 for "  city ev " against "City EV" |
| 4 | `GET /api/car-types/category/{category}` returns only that category | Met. ELECTRIC -> 1 type, COMMERCIAL -> 1, STANDARD -> 7; unknown category -> 400 |
| 5 | Existing tests give the same result as at baseline, with no existing test file edited | Met, with a caveat: 182 of the 183 existing tests pass both before and after. One test already fails at the baseline (see metrics) |
| 6 | New tests cover the three rule classes, the factory, the service and the validator | Met. 24 new tests, 24 passed |

Out of scope: pricing, booking, the return invoice and the seed data. The deposit is stored and reported but is not yet charged on a booking.

**Before and after.**
Before: `dfc61ab8dd6f9bbb2301171457dc03946a249576` (tag `baseline`). After: `7af8d229edfaa1d5c933a0a56aa267a443bece9f` (tag `ch02`, branch `ch02-vehicle-categories`), a single commit on top of the baseline. 21 files changed, 16 added and 5 modified, 1,226 lines added and 0 removed. Paths are under `WanderCar_DotNet/src/`.

- Modified (lines only added): `FlemanApi/Models/CarType.cs`, `FlemanApi/DTO/CarTypeDTO.cs`, `FlemanApi/Data/FlemanDbContext.cs`, `FlemanApi/Program.cs`, `FlemanApi/Migrations/FlemanDbContextModelSnapshot.cs`.
- Added, production: `FlemanApi/Models/VehicleCategory.cs`; `FlemanApi/Service/Categories/IVehicleCategoryRules.cs`, `StandardCategoryRules.cs`, `ElectricCategoryRules.cs`, `CommercialCategoryRules.cs`, `VehicleCategoryRulesFactory.cs`; `FlemanApi/Service/ICarTypeService.cs`, `CarTypeService.cs`; `FlemanApi/Controllers/CarTypeController.cs`; `FlemanApi/DTO/CreateCarTypeRequest.cs`; `FlemanApi/Validators/CreateCarTypeRequestValidator.cs`; `FlemanApi/Migrations/20261008070000_AddVehicleCategory.cs` and `.Designer.cs`.
- Added, tests: `FlemanApi.Tests/Services/VehicleCategoryRulesTests.cs`, `FlemanApi.Tests/Services/CarTypeServiceTests.cs`, `FlemanApi.Tests/Validators/CreateCarTypeRequestValidatorTests.cs`.

Full list: `Part2/changes/Change2/changed_files.txt`; diff: `Part2/changes/Change2/diff.patch`.

**Implementation and tests.**
What changed: a `VehicleCategory` enum; `Category` (default STANDARD) and `SecurityDeposit` on `CarType` and `CarTypeDTO`; an `IVehicleCategoryRules` interface with one rules class per category, created by `VehicleCategoryRulesFactory.Create(category)`; a `CarTypeService` and `CarTypeController` exposing `POST /api/staff/car-types` and `GET /api/car-types/category/{category}`; a request DTO with a FluentValidation validator; an EF Core migration adding the two columns with defaults `STANDARD` and `0`; two DI registrations in `Program.cs`.

Run command (in `WanderCar_DotNet/`, .NET SDK 8.0.425): `dotnet test src/FlemanApi.Tests/FlemanApi.Tests.csproj`. The test project is named explicitly because the solution file is `.slnx`, which SDK 8 cannot open.

Actual result: build 0 errors, 0 warnings. Total 207, passed 206, failed 1, skipped 0. Log: `Part2/changes/Change2/test_result.txt`. The baseline run with the same command gave total 183, passed 182, failed 1 (`Part2/changes/Change2/baseline_test.log`). The failing test is the same in both runs, `StaffServiceTests.ProcessReturnAsync_CreatesInvoiceHeaderAndDetails`, and is not related to CH02 (explained under metrics).

Additional run-time check against MySQL 8.0.46: `dotnet ef migrations has-pending-model-changes` reported no pending changes; the migration was applied to a database created by the baseline build; HTTP requests against the running application exercised every acceptance criterion. Logs: `Part2/changes/Change2/http_evidence.txt` and `db_after.txt`.

**Pattern effects.**

| Instance name | Effect | Reason |
|---|---|---|
| Factory Method (parameterized) - Vehicle Category Rules Creation | newly introduced | Creator `IVehicleCategoryRulesFactory` / `VehicleCategoryRulesFactory.Create(VehicleCategory)`; Product `IVehicleCategoryRules`; ConcreteProducts `StandardCategoryRules`, `ElectricCategoryRules`, `CommercialCategoryRules`; Client `CarTypeService.CreateCarTypeAsync`. The service never names a concrete rules class |
| Repository - Generic Repository | extended | New client `CarTypeService` uses `IGenericRepository<CarType, long>`; interface and implementation unchanged |
| Dependency Injection / IoC - ASP.NET Core DI Composition | extended | Two new registrations in `Program.cs:189-192`; new constructor-injected service and controller |
| DTO - API Request/Response DTO Boundary | modified | `CarTypeDTO` gains `Category` and `SecurityDeposit`; new `CreateCarTypeRequest`. `MappingProfile` unchanged |
| Service Layer - Booking Service Layer | preserved | Not touched; the new controller/service pair repeats the layering but is not part of this instance |
| Middleware Pipeline / Chain of Responsibility - ASP.NET Core Request Middleware Chain | preserved | Not touched; the new endpoints' 400/409 responses come from the existing exception middleware |
| Producer-Consumer - Email Background Work Queue; Adapter - Java Microservice Adapter; Proxy - Legacy API Proxy; Strangler Fig - Legacy Java Integration | preserved | Not touched |

No instance was replaced or removed. Classification note: the new instance is the parameterized variant of Factory Method (one creator method selecting the product from a parameter). It has a single concrete creator and no creator hierarchy, so some texts would call it Simple Factory. File and line ranges for every participant are in `Part2/changes/Change2/pattern_effect.md`.

**Refactoring.**
No existing code was refactored in CH02, and none is claimed. The change is purely additive: the five modified files gained 32 lines and lost none, and no existing test file was edited. Two real smells were observed and deliberately left alone because they lie outside this change's scope: (1) a hidden dependency on the system clock, `StaffService.ProcessReturnAsync` reads `DateTime.Now` (`Service/StaffService.cs:124`), which makes one existing test non-deterministic; the fitting technique is to inject a clock. (2) Duplicated code, `BookingService.DaysBetween` and `StaffService.DaysBetween` have identical bodies; the fitting technique is Extract Method into a shared helper. One design decision avoided a refactoring: the staff-only endpoint was placed in a new `CarTypeController`, because `VehicleController` has a class-level `[AllowAnonymous]` that would override `[Authorize]` on any action added to it.
Behaviour-preservation evidence: 0 deleted lines in the diff; the 183 existing tests give an identical result before and after (182 pass, the same 1 fails); existing endpoints return the same data on a database upgraded from the baseline.

**Before/after metrics.**
Same scope, tool and commands at both commits.

| Metric | Scope and tool | Before | After |
|---|---|---|---|
| Source LOC | `FlemanApi/**/*.cs` excluding `Migrations`, `obj`, `bin`; non-blank lines not starting with `//`; `find` + `grep` + `wc` | 3,599 | 3,794 (+195) |
| Source files | same scope | 107 | 118 (+11) |
| Test LOC | `FlemanApi.Tests/**/*.cs`, same rule | 2,136 | 2,383 (+247) |
| Tests total | `dotnet test`, SDK 8.0.425 | 183 | 207 (+24) |
| Tests passed | same | 182 | 206 |
| Tests failed | same | 1 | 1 (same test) |
| Build warnings / errors | same | 0 / 0 | 0 / 0 |
| Lines added / removed | `git diff --numstat baseline ch02` | - | 1,226 / 0 |

Of the 1,226 added lines, 645 are EF Core migration files (593 in the designer snapshot) and 309 are tests.
Explanation of the failing test: `ProcessReturnAsync_CreatesInvoiceHeaderAndDetails` fixes the hand-over date at 2026-08-01 while the service bills up to the current time, so the expected add-on amount of 40.0 is only produced on 2026-08-05. On the run date (2026-10-10) it gives 710.0. It fails identically at the baseline, so it is a pre-existing defect and not a regression.
Missing measures: code coverage, cyclomatic complexity and coupling/cohesion were not measured at either commit, so no value is reported for them.

## 3.1 Updated pattern counts

Rows for CH02 applied directly on the baseline. If another change is merged before it, add that change's instances to both snapshots; CH02 itself adds exactly one type and one instance.

| Snapshot | Distinct pattern types | Instances | Concrete participant classes added | Rejected / uncertain observations |
|---|---|---|---|---|
| Baseline `dfc61ab8` | 9 | 9 | - | as recorded in Part 1 |
| After CH02 `7af8d229` | 10 | 10 | 4: `VehicleCategoryRulesFactory` (concrete creator), `StandardCategoryRules`, `ElectricCategoryRules`, `CommercialCategoryRules` (concrete products); plus interfaces `IVehicleCategoryRulesFactory`, `IVehicleCategoryRules` | Uncertain: Factory Method vs Simple Factory label. Rejected: `CarTypeController`/`CarTypeService` as a second Service Layer instance, since it only repeats the existing layering |

All nine baseline instances keep their names because their collaborations continue: Generic Repository, ASP.NET Core DI Composition, Booking Service Layer, ASP.NET Core Request Middleware Chain, Email Background Work Queue, Java Microservice Adapter, Legacy API Proxy, Legacy Java Integration, API Request/Response DTO Boundary. The new instance, Vehicle Category Rules Creation, has no predecessor: the baseline had no category concept, so it replaces nothing.

**Interpretation.**
Benefits: the rules that differ by category sit in one small class each, and the category-to-class mapping is in one `switch`. Adding a category means one enum member, one rules class and one `switch` arm; `CarTypeService`, the controller and the validator do not change. The rules classes have no dependencies, so they are tested without mocks.
Trade-offs: 195 more source lines and 11 more files for two rules (a deposit multiplier and one mandatory field), which a single `if` could also express today; the indirection only pays off if categories keep being added or their rules grow. The `switch` still has to be edited for each new category, so the design is closed against changes to clients but not to the factory itself. The category is stored twice in meaning, as an enum value and as a rules class, and the two must be kept in step; a test enforces this for every enum value.
Remaining limitations: the deposit is computed once at creation and stored, so a later change to a multiplier or to the daily rate does not update existing rows; the deposit is not yet used in booking or invoicing; there is no endpoint to change the category of an existing car type; the six seeded types are all STANDARD.
The higher LOC does not by itself show a better or worse design. What the evidence does show is narrower: the change added a variation point without modifying or deleting any existing line, and the existing tests behave exactly as before.
