# Dependency Injection in Python

This folder is the Python port of the **Dependency Injection** pattern instance **"Service Registration and Constructor Injection"** from the WanderCar / Fleman .NET backend. It follows the same specification and test cases (D01–D09, D04b) as the [JavaScript version](../javascript/src/dependency_injection.mjs).

## What it models

In the original C#:
- `FlemanApi/Program.cs:179-214` registers every service with the built-in .NET container. For example, `AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>))` at line 179 and `AddSingleton(TimeProvider.System)` at line 213.
- Each service receives its dependencies through its constructor, for example `StaffService(...)` at `FlemanApi/Service/StaffService.cs:33`.

This port is a minimal container with the same behaviour:

| Behaviour | Where in `dependency_injection.py` |
|---|---|
| Singleton / scoped / transient lifetimes | `Lifetime`, `ServiceProvider.get_required_service` |
| Constructor injection | `ServiceCollection._add` (calls `implementation(*resolved_deps)`) |
| Open generics: `IGenericRepository<Car>` closes the `IGenericRepository` registration | `ServiceCollection.add_open_generic`, `ServiceProvider._lookup` |
| Last registration wins | `ServiceCollection._add` (dict assignment) |
| Scope validation: a scoped service cannot be resolved from the root | `ServiceProvider.get_required_service` |
| Errors use the same messages as .NET | `LookupError` |

## Files

```
python/
├── dependency_injection.py        Lifetime, ServiceCollection, ServiceProvider
└── test_dependency_injection.py   unittest, 10 tests
```

## Requirements

- Python 3.10 or later. The code uses `X | None` type hints. It was tested with **Python 3.13.7**.
- Standard library only: `unittest`, `re`, `dataclasses`, `enum`. Nothing to install.

## Run the tests

From `Part3/python`:

```bash
python -m unittest -v test_dependency_injection
# or
python test_dependency_injection.py
```

You can also run `bash Part3/tools/run_di_all.sh` to build and test all four languages and write the logs.

**Expected result:** `Ran 10 tests ... OK`. The actual log is in [`../logs/python_dependency_injection_test.log`](../logs/python_dependency_injection_test.log).

## Test cases

| ID | Checks |
|---|---|
| D01 | Constructor injection resolves the whole graph: `MaintenanceService(IGenericRepository<MaintenanceSchedule>, IGenericRepository<Car>, TimeProvider)` |
| D02 | Scoped: one instance per scope, and a different one in each new scope |
| D03 | Services in one scope share that scope's `DbContext`, so there is one unit of work per request |
| D04 | An instance-registered singleton is the same object everywhere |
| D04b | A class-registered singleton is built once and shared by the root and every scope |
| D05 | Transient: a new instance on every resolve |
| D06 | An open generic closes per type argument (`<Car>` and `<Hub>` give different instances) |
| D07 | An unregistered service gives `No service for type 'X' has been registered.` |
| D08 | A scoped service resolved from the root gives `Cannot resolve scoped service 'X' from root provider.` |
| D09 | The last registration wins (`JavaInvoicePdfService` replaces `InvoicePdfService`) |

## Python-specific notes

- **Classes and factories are interchangeable.** A class is just a callable, so "construct the implementation" is simply `implementation(*deps)`. A plain factory function could be registered in exactly the same way.
- **Dependencies are listed explicitly.** Python could auto-wire them from constructor type hints (`inspect.signature` and `typing.get_type_hints`). The port keeps explicit token lists so all four languages register services the same way.
- **Duck typing:** there are no interfaces. `"IMaintenanceService"` is only a token, and any object returned for it is accepted. The tests use `assertIsInstance` to check the concrete type.
- **Instance registrations need their own method.** `add_singleton_instance` is separate for the same reason as in Java: a class object is itself a valid instance, so one method that accepted both would be ambiguous.
- **Not thread-safe:** the instance caches are plain `dict`s. The .NET container is thread-safe.
- **Docstrings in the LOC count:** the team LOC rule counts docstring lines because they are not `#` comments. That is why `cross_language.csv` shows 81 team-rule LOC but 67 lizard NLOC (lizard excludes docstrings).
