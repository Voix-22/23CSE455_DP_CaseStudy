# CH02 - Add new vehicle categories

Owner: Bhavana P H (team B8). Before: `dfc61ab8dd6f9bbb2301171457dc03946a249576` (tag `baseline`). After: see `after_commit.txt` (branch `ch02-vehicle-categories`, tag `ch02`).

## Baseline behaviour

A "category" was only a row in `car_types` with a name and three rates (seeded: Economy, Compact, Sedan, SUV, Luxury, Minivan). The code had no category concept, no category-specific rule and no endpoint to create a car type.

## Requirement

Every car type belongs to a vehicle category. Two new categories, **ELECTRIC** and **COMMERCIAL**, are added alongside **STANDARD**, which covers every existing car type. Each category has its own rules. Staff can create a car type in any category; anyone can list car types by category.

## Category rules

These values were chosen by the team for the case study; they are not taken from the original application.

| Category | Security deposit | Rate rule |
|---|---|---|
| STANDARD | 0 | none |
| ELECTRIC | 2 x daily rate | none |
| COMMERCIAL | 5 x daily rate | weekly rate is mandatory |

## Acceptance criteria and observed result

| # | Criterion | Observed | Evidence |
|---|---|---|---|
| 1 | Existing car types keep working and are reported as `STANDARD` with deposit 0. | Met | `http_evidence.txt` B1, B14; test `Mapping_CarTypeWithoutExplicitCategory_IsReportedAsStandard` |
| 2 | `POST /api/staff/car-types` (role STAFF) creates a car type in any of the three categories and stores the deposit computed by that category's rules. | Met | `http_evidence.txt` B3, B5, B6, B8, B9; `CarTypeServiceTests` |
| 3 | A COMMERCIAL car type without a weekly rate is rejected with 400; a duplicate name (ignoring case and surrounding spaces) is rejected with 409. | Met | `http_evidence.txt` B4, B7; `CarTypeServiceTests` |
| 4 | `GET /api/car-types/category/{category}` returns only car types of that category. | Met | `http_evidence.txt` B12-B14; `GetCarTypesByCategoryAsync_ReturnsOnlyThatCategory` |
| 5 | Every existing test gives the same result as at baseline, with no edit to any existing test file. | Met, with a caveat | `baseline_test.log`, `test_result.txt`, `changed_files.txt` |
| 6 | New tests cover the three rule classes, the factory, the service and the validator. | Met: 24 new tests, 24 passed | `test_result.txt` |

Caveat on criterion 5. It was first written as "all 183 existing tests pass". That cannot be met by any change, because one baseline test already fails before CH02 is applied: `StaffServiceTests.ProcessReturnAsync_CreatesInvoiceHeaderAndDetails`. The test hands the car over on the fixed date 2026-08-01 and `StaffService.ProcessReturnAsync` bills up to `DateTime.Now`, so the expected add-on amount (40.0, four days) is only produced on 2026-08-05. Baseline: 182 passed, 1 failed of 183. After CH02: 206 passed, 1 failed of 207. The failing test, its message and its cause are identical in both runs, and CH02 does not touch `StaffService` or its tests.

## Out of scope

Pricing, booking, the return invoice and the seed data are not touched, so that the team's other changes can be merged without overlap. `VehicleService` and its tests are unchanged. The security deposit is stored and reported; it is not yet charged on a booking or shown on an invoice.

## Design

- `Models/VehicleCategory.cs`: enum `STANDARD`, `ELECTRIC`, `COMMERCIAL`.
- `Models/CarType.cs`: new `Category` (default `STANDARD`) and `SecurityDeposit`.
- `Service/Categories/`: `IVehicleCategoryRules` with `StandardCategoryRules`, `ElectricCategoryRules`, `CommercialCategoryRules`, created by `VehicleCategoryRulesFactory.Create(category)`.
- `Service/CarTypeService.cs` behind `ICarTypeService`; `Controllers/CarTypeController.cs`; `DTO/CreateCarTypeRequest.cs`; `Validators/CreateCarTypeRequestValidator.cs`.
- `DTO/CarTypeDTO.cs`: new `Category` and `SecurityDeposit`.
- `Data/FlemanDbContext.cs`: `Category` stored as a string with default `STANDARD`; migration `AddVehicleCategory` adds both columns.
- `Program.cs`: two registrations in a block marked CH02.
