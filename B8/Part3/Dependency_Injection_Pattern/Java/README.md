# Dependency Injection in Java

This folder is the Java port of the **Dependency Injection** pattern instance **"Service Registration and Constructor Injection"** from the WanderCar / Fleman .NET backend. It follows the same specification and test cases (D01–D09, D04b) as the [JavaScript version](../javascript/src/dependency_injection.mjs).

## What it models

In the original C#:
- `FlemanApi/Program.cs:179-214` registers every service with the built-in .NET container. For example, `AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>))` at line 179 and `AddSingleton(TimeProvider.System)` at line 213.
- Each service receives its dependencies through its constructor, for example `StaffService(...)` at `FlemanApi/Service/StaffService.cs:33`.

This port is a minimal container with the same behaviour:

| Behaviour | Where |
|---|---|
| Singleton / scoped / transient lifetimes | `Lifetime`, `ServiceProvider.getRequiredService` |
| Constructor injection | `ServiceCollection.add` (via reflection on the public constructor) |
| Open generics: `IGenericRepository<Car>` closes the `IGenericRepository` registration | `ServiceCollection.addOpenGeneric`, `ServiceProvider.lookup` |
| Last registration wins | `ServiceCollection.add` (map `put`) |
| Scope validation: a scoped service cannot be resolved from the root | `ServiceProvider.getRequiredService` |
| Errors use the same messages as .NET | `IllegalStateException` (in .NET, `InvalidOperationException`) |

## Files

```
java/
├── src/main/java/di/
│   └── DependencyInjection.java     the whole pattern, as nested classes:
│         Lifetime                    enum SINGLETON | SCOPED | TRANSIENT
│         ServiceCollection           registration API (Program.cs role)
│         ServiceProvider             root provider and scopes, resolution
├── src/test/java/di/
│   └── DependencyInjectionTest.java   JUnit 5, 10 tests
└── lib/
    └── junit-platform-console-standalone-1.11.4.jar
```

Where the JUnit jar comes from:
- **Source:** https://repo1.maven.org/maven2/org/junit/platform/junit-platform-console-standalone/1.11.4/
- **SHA-256:** `b016ef6b1c3454d6d7c2c88ce081dabf289699686af6622d6e4e2e1b54b4a2fc`

## Requirements

- JDK 17 or later. The code uses records and pattern-matching `instanceof`. It was tested with **javac / java 24.0.2**.
- The JUnit jar in `lib/`. No Maven or Gradle is needed.

## Build and run the tests

From `Part3/java`. The classpath separator is `;` on Windows and `:` on Linux/macOS.

```bash
# Windows (Git Bash / PowerShell)
javac -d out/main src/main/java/di/*.java
javac -d out/test -cp "out/main;lib/junit-platform-console-standalone-1.11.4.jar" src/test/java/di/*.java
java -jar lib/junit-platform-console-standalone-1.11.4.jar execute --class-path "out/main;out/test" --select-class di.DependencyInjectionTest

# Linux / macOS: same commands with ':' in place of ';'
```

You can also run `bash Part3/tools/run_di_all.sh` to build and test all four languages and write the logs.

**Expected result:** `10 tests successful, 0 tests failed`. The actual log is in [`../logs/java_dependency_injection_test.log`](../logs/java_dependency_injection_test.log).

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

## Java-specific notes

- **One file.** The pattern lives in a single file, like `dependency_injection.{mjs,py,hpp}` in the other ports. Java allows only one public top-level class per file, so `Lifetime`, `ServiceCollection` and `ServiceProvider` are nested inside `DependencyInjection`. Callers import them as `di.DependencyInjection.ServiceCollection` and so on.
- **String tokens:** tokens are strings, not `Class<?>` objects, because of type erasure. At run time `IGenericRepository<Car>` and `IGenericRepository<Hub>` are the same `Class`, so an open generic cannot be keyed by type. `getRequiredService(token, Class<T>)` adds the typed cast that .NET's `GetRequiredService<T>()` gives you.
- **Constructor injection by reflection:** this uses `Constructor.newInstance`, the closest of the four ports to .NET's reflection-based activation. Dependencies are still listed as tokens, so all four ports use the same registration shape.
- **Overload pitfall caught by D04b:** the first draft had `addSingleton(String, Object instance)` next to `addSingleton(String, Class<?>, String...)`. Java chooses the non-varargs overload first, so `addSingleton("X", Foo.class)` registered the `Class` object itself as the singleton. The instance overload is now named `addSingletonInstance`.
- **Not thread-safe:** the instance caches are plain `HashMap`s, like the JavaScript and Python ports. The .NET container is thread-safe.
