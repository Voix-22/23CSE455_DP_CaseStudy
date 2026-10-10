# Dependency Injection in C++

This folder is the C++17 port of the **Dependency Injection** pattern instance **"Service Registration and Constructor Injection"** from the WanderCar / Fleman .NET backend. It follows the same specification and test cases (D01–D09, D04b) as the [JavaScript version](../javascript/src/dependency_injection.mjs).

## What it models

In the original C#:
- `FlemanApi/Program.cs:179-214` registers every service with the built-in .NET container. For example, `AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>))` at line 179 and `AddSingleton(TimeProvider.System)` at line 213.
- Each service receives its dependencies through its constructor, for example `StaffService(...)` at `FlemanApi/Service/StaffService.cs:33`.

This port is a minimal, header-only container with the same behaviour:

| Behaviour | Where in `dependency_injection.hpp` |
|---|---|
| Singleton / scoped / transient lifetimes | `di::Lifetime`, `ServiceProvider::resolve` |
| Constructor injection | `ServiceCollection::add<Service, Impl, Deps...>`, which builds `make_shared<Impl>(get<Deps>(token)...)` at compile time |
| Open generics: `IGenericRepository<Car>` closes the `IGenericRepository` registration | `ServiceCollection::addOpenGeneric`, `ServiceProvider::lookup` |
| Last registration wins | `insert_or_assign` |
| Scope validation: a scoped service cannot be resolved from the root | `ServiceProvider::resolve` |
| Errors use the same messages as .NET | `std::logic_error` |

## Files

```
cpp/
├── dependency_injection.hpp         header-only container (namespace di)
└── test_dependency_injection.cpp    10 tests and a small built-in test runner (TAP-style output)
```

There is no test-framework dependency. The test file has a harness of about 25 lines (`TEST`, `CHECK`, `CHECK_THROWS_MESSAGE`), so it builds with nothing but a C++17 compiler.

## Requirements

- A C++17 compiler: g++ 8 or later, clang 7 or later, or MSVC 2019 or later.
- It was tested with **clang 21.1.0**, provided by `zig 0.16.0` (`pip install ziglang`), because this Windows machine has no g++ or MSVC. Any standard compiler works.

## Build and run the tests

From `Part3/cpp`:

```bash
# g++ / clang
g++ -std=c++17 -O2 -Wall -Wextra -o test_dependency_injection test_dependency_injection.cpp
./test_dependency_injection

# Windows without g++/MSVC: zig's bundled clang, as used for the logged run
pip install ziglang
python -m ziglang c++ -std=c++17 -O2 -Wall -Wextra -o test_dependency_injection.exe test_dependency_injection.cpp
./test_dependency_injection.exe

# MSVC (Developer Command Prompt)
cl /std:c++17 /EHsc /O2 /W4 test_dependency_injection.cpp && test_dependency_injection.exe
```

You can also run `CXX="python -m ziglang c++" bash Part3/tools/run_di_all.sh` to build and test all four languages and write the logs.

**Expected result:** `# tests 10`, `# pass 10`, `# fail 0`, exit code 0, and no compiler warnings from the project files. The actual log is in [`../logs/cpp_dependency_injection_test.log`](../logs/cpp_dependency_injection_test.log).

> **About the first zig build:** zig compiles its bundled libc++ the first time, and that prints nullability warnings from libc++'s own headers. Those warnings are not from this project. `run_di_all.sh` filters them out and records the count of warnings from the project files, which is 0.

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

## C++-specific notes

- **No runtime reflection.** Constructor injection is expressed at compile time, so the dependency *types* appear in the registration:

  ```cpp
  services.addScoped<IMaintenanceService, MaintenanceService, GenericRepository, GenericRepository, TimeProvider>(
      "IMaintenanceService", {"IGenericRepository<MaintenanceSchedule>", "IGenericRepository<Car>", "TimeProvider"});
  ```

  A wrong constructor signature is a compile error, not a runtime failure. A mismatch between the number of tokens and the number of dependency types throws when the service is registered.
- **Type-erased storage with a checked cast.** Instances are kept as `std::shared_ptr<void>` together with the `std::type_index` of the service type they were registered as. `getRequiredService<T>()` refuses a mismatched `T` instead of performing an unchecked cast.
- **Ownership:** `shared_ptr` reproduces .NET's shared, garbage-collected references. A scope keeps a raw pointer to its root, so **the root provider must outlive its scopes**, just as an `IServiceScope` must not outlive its `ServiceProvider`.
- **Not thread-safe:** the instance caches are plain `std::map`s. The .NET container is thread-safe.
