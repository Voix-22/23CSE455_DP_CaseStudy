# CH02 - Refactoring

**No existing code was refactored in this change.**

CH02 is additive. The five edited files only gain lines; no existing line was changed or removed (`git diff --numstat baseline ch02` reports 0 deletions for every file, see `supporting/numstat.txt`):

| File | Lines added | Lines removed | Nature of the edit |
|---|---|---|---|
| `FlemanApi/Models/CarType.cs` | 5 | 0 | two new properties |
| `FlemanApi/DTO/CarTypeDTO.cs` | 5 | 0 | two new properties |
| `FlemanApi/Data/FlemanDbContext.cs` | 7 | 0 | one new property configuration |
| `FlemanApi/Program.cs` | 5 | 0 | two new registrations |
| `FlemanApi/Migrations/FlemanDbContextModelSnapshot.cs` | 10 | 0 | two new columns in the model snapshot |

## Design decision that avoided a refactoring

The staff-only create endpoint was put in a new `CarTypeController` instead of the existing `VehicleController`. `VehicleController` carries a class-level `[AllowAnonymous]` (`Controllers/VehicleController.cs:9`), which overrides `[Authorize]` on any action inside it, so adding the endpoint there would have required restructuring that controller's authorization. Likewise the new logic went into a new `CarTypeService` rather than `VehicleService`, so no existing constructor signature and no existing test file had to change.

## Behaviour-preservation evidence

- No existing production line was modified or deleted (table above; see `diff.patch`).
- No existing test file was modified (`changed_files.txt` lists only added test files).
- The 183 existing tests give the same result before and after: 182 passed and 1 failed at baseline (`baseline_test.log`), and the same 182 pass and the same 1 fails after CH02 (`test_result.txt`, 206 passed of 207, the other 24 being new).
- At run time, a database created and seeded by the baseline build was upgraded by the CH02 build: the six existing car types and the existing `GET /api/car-types` and `GET /api/cars/available` endpoints return the same data as before plus the two new fields (`http_evidence.txt` A1, B1, B18; `db_after.txt`).

## Smells observed but deliberately left alone

1. **Non-deterministic test caused by a hidden dependency on the system clock.** `StaffService.ProcessReturnAsync` reads `DateTime.Now` directly (`Service/StaffService.cs:124` at baseline), and `StaffServiceTests.ProcessReturnAsync_CreatesInvoiceHeaderAndDetails` asserts an amount that is only correct on 2026-08-05. This is the one failing test in both runs. The fitting refactoring is to inject a clock (`TimeProvider`) into `StaffService`. It was not done here because `StaffService` and the return invoice are outside the scope of CH02 and the fix would require editing an existing test file.
2. **Duplicated code.** `BookingService.DaysBetween` and `StaffService.DaysBetween` are identical private methods. They belong to pricing, which is another team member's change.
