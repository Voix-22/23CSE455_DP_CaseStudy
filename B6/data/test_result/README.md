# Baseline test evidence

Build and test output for the unmodified baseline in `data/code/WanderCar_DotNet/`.
Everything here comes from one actual run on 2026-10-11; nothing is estimated.

## Result

| | |
|---|---|
| Build | Succeeded, 0 warnings, 0 errors |
| Tests run | 183 |
| Passed | 182 |
| Failed | 1 |
| Test time | 13.25 s |

## Environment

- Windows 11 Pro 10.0.26300
- .NET SDK 8.0.425, runtime 8.0.31
- NUnit 3.14.0, NUnit3TestAdapter 4.5.0, FluentAssertions 6.12.1, Moq 4.20.72
- No MySQL needed: the tests use mocks and the EF Core in-memory provider

Full details are in `environment.txt`.

## Commands

Run from the `A1/` folder.

```powershell
$proj = 'data\code\WanderCar_DotNet\src\FlemanApi.Tests\FlemanApi.Tests.csproj'
dotnet restore $proj --source https://api.nuget.org/v3/index.json
dotnet build $proj --no-restore
dotnet test $proj --no-build --logger "trx;LogFileName=baseline_tests.trx" --logger "console;verbosity=normal"
```

`--artifacts-path <temp folder>` was added to each command so that `bin/` and `obj/` were written outside the source snapshot.

Two notes on reproducing this:

- `dotnet build DotnetBackend.slnx` fails on SDK 8 with `MSB4068`, because the `.slnx` solution format needs SDK 9.0.200 or later. The test project was built directly instead; it references `FlemanApi.csproj`, so both projects are compiled. See `slnx_attempt.txt`.
- The machine had no NuGet package source configured, so `--source` was passed to `dotnet restore`.

## Results by test class

| Module | Test class | Total | Passed | Failed |
|---|---|---:|---:|---:|
| Repository | AvailabilityRepositoryTests | 7 | 7 | 0 |
| Repository | GenericRepositoryTests | 10 | 10 | 0 |
| Services | AddonServiceTests | 2 | 2 | 0 |
| Services | AirportServiceTests | 16 | 16 | 0 |
| Services | BookingServiceTests | 21 | 21 | 0 |
| Services | CustomerServiceTests | 21 | 21 | 0 |
| Services | EmailServiceTests | 3 | 3 | 0 |
| Services | GenericServiceTests | 8 | 8 | 0 |
| Services | LocationServiceTests | 12 | 12 | 0 |
| Services | StaffServiceTests | 24 | 23 | 1 |
| Services | VehicleServiceTests | 11 | 11 | 0 |
| Util | RentalRateCalculatorTests | 8 | 8 | 0 |
| Validators | AddonLineRequestValidatorTests | 4 | 4 | 0 |
| Validators | AirportRequestValidatorTests | 8 | 8 | 0 |
| Validators | CreateBookingRequestValidatorTests | 10 | 10 | 0 |
| Validators | GuestCustomerRequestValidatorTests | 4 | 4 | 0 |
| Validators | LoginRequestValidatorTests | 4 | 4 | 0 |
| Validators | RegisterRequestValidatorTests | 7 | 7 | 0 |
| Validators | StaffLoginRequestValidatorTests | 3 | 3 | 0 |
| | **Total** | **183** | **182** | **1** |

## The failing test

`StaffServiceTests.ProcessReturnAsync_CreatesInvoiceHeaderAndDetails` (TC111 in `testcases.csv`)

```
Expected result.AddonAmount to be 40.0, but found 720.0 (difference of 680).
```

The test depends on the date it is run:

- The fixture sets the handover date to 2026-08-01 (`StaffServiceTests.cs` line 196).
- `StaffService.ProcessReturnAsync` takes the return time from `DateTime.Now` (`StaffService.cs` line 124) and bills add-ons as rate × quantity × days (line 133).
- The test expects 40.0, which is 10.0 × 1 × 4 days. That holds only if the test runs within four days of 2026-08-01.
- On 2026-10-11 the gap is 72 days, so the result is 10.0 × 1 × 72 = 720.0.

This is a defect in the baseline test, not a change we made. The baseline was left unmodified.

## Files

| File | Contents |
|---|---|
| `testcases.csv` | All 183 test cases: ID, module, class, method, result, duration, error message |
| `test_summary.csv` | Totals per test class (the table above) |
| `baseline_tests.trx` | Raw test results file written by `dotnet test` |
| `test_output.txt` | Console output of `dotnet test` |
| `build_output.txt` | Console output of `dotnet build` |
| `restore_output.txt` | Console output of `dotnet restore` |
| `slnx_attempt.txt` | Output of the failed `.slnx` build attempt |
| `environment.txt` | OS, SDK and runtime versions |
| `screenshots/` | Images of the output above |

## Screenshots

The images in `screenshots/` are rendered from the captured console text in the `.txt` files. They are not captures of a terminal window. Temp-folder and absolute paths are shortened in the images; the `.txt` files keep the full paths.

| Image | Shows |
|---|---|
| `01_environment.png` | OS, SDK and runtime versions |
| `02_slnx_not_supported_by_sdk8.png` | The `.slnx` build error |
| `03_restore_and_build_success.png` | Restore and successful build |
| `04_test_run_start.png` | Start of the test run |
| `05_failed_test_detail.png` | The failing test and its error message |
| `06_test_summary.png` | End of the test run with totals |
| `07_results_by_test_class.png` | Results per test class |
