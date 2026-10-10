# Common specification - Factory Method (parameterized): Vehicle Category Rules Creation

Source instance: `VehicleCategoryRulesFactory.Create(VehicleCategory)` returning `IVehicleCategoryRules` in the C# case study (`WanderCar_DotNet/src/FlemanApi/Service/Categories/`, commit `7af8d229edfaa1d5c933a0a56aa267a443bece9f`). Every language folder implements exactly this specification and nothing else.

## Participants

| Role | Name | Members |
|---|---|---|
| Enumeration | `VehicleCategory` | `STANDARD`, `ELECTRIC`, `COMMERCIAL` |
| Product (abstraction) | vehicle category rules | `category()`, `calculateSecurityDeposit(dailyRate)`, `validateRates(dailyRate, weeklyRate, monthlyRate)` |
| ConcreteProduct | standard, electric and commercial category rules | one class per category |
| Creator | vehicle category rules factory | `create(category)` |

Names follow each language's naming convention (for example `calculate_security_deposit` in Python).

## Rules

| Category | `calculateSecurityDeposit(dailyRate)` | `validateRates(dailyRate, weeklyRate, monthlyRate)` |
|---|---|---|
| STANDARD | `0` | no errors |
| ELECTRIC | `dailyRate * 2` | no errors |
| COMMERCIAL | `dailyRate * 5` | one error if `weeklyRate` is absent, otherwise no errors |

- `validateRates` returns a list of error strings; an empty list means valid.
- `weeklyRate` and `monthlyRate` are optional; "none" below means the language's way of saying "no value".
- The only error message is exactly: `weeklyRate is required for COMMERCIAL car types`
- `create(category)` returns the rules object of that category and is the only place that names the concrete classes.

## Error case

`create` rejects an unknown category with an error (exception). What "unknown" means depends on what the language's enumeration allows; each README states the value used.

## Test cases (identical in all four languages)

| # | Input | Expected |
|---|---|---|
| 1 | `create(STANDARD)`; `deposit(100)`; `validateRates(100, none, none)` | standard rules object; `0`; empty list |
| 2 | `create(ELECTRIC)`; `deposit(60)`; `validateRates(60, none, none)` | `120`; empty list |
| 3 | `create(COMMERCIAL)`; `deposit(80)` | `400` |
| 4 | COMMERCIAL `validateRates(80, none, none)` | exactly one error: `weeklyRate is required for COMMERCIAL car types` |
| 5 | COMMERCIAL `validateRates(80, 500, none)` | empty list |
| 6 | for every category: `create(category).category()` | equals that category |
| 7 | `create(<unknown category>)` | rejected with an error |

## Test runner contract

No test framework. One line per case, `PASS <n> <description>` or `FAIL <n> <description>`, then a final line `PASS <passed>/7`. Exit code 0 only if all seven pass, otherwise 1.
