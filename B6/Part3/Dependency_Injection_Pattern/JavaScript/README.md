# Part 3: JavaScript implementations

This folder holds JavaScript re-implementations of the six pattern types confirmed in the WanderCar / Fleman .NET backend, one representative instance each. Each module reproduces the behaviour of the original C# participants, and the tests check that behaviour, including the exact error messages and a few deliberate quirks of the original code.

The **Dependency Injection** module is also ported to [Java](../java/README.md), [Python](../python/README.md) and [C++](../cpp/README.md), with the same ten test cases.

## Modules

| Pattern type | Source instance (C#) | Module | Tests | Cases |
|---|---|---|---|---|
| Repository | Generic Entity Repository: `Repository/IGenericRepository.cs`, `GenericRepository.cs` | `src/repository.mjs` | `test/repository.test.mjs` | R01–R08 (8) |
| Dependency Injection | Service Registration and Constructor Injection: `Program.cs:179-214` | `src/dependency_injection.mjs` | `test/dependency_injection.test.mjs` | D01–D09, D04b (10) |
| Chain of Responsibility | HTTP Middleware Pipeline: `Middleware/ExceptionHandlingMiddleware.cs`, `RequestLoggingMiddleware.cs` | `src/chain_of_responsibility.mjs` | `test/chain_of_responsibility.test.mjs` | C01–C07 (7) |
| Producer–Consumer | Background Email Queue: `Service/EmailBackgroundQueue.cs`, `EmailService.cs`, `EmailQueueHostedService.cs` | `src/producer_consumer.mjs` | `test/producer_consumer.test.mjs` | P01–P07 (7) |
| Adapter | Java Invoice PDF Adapter: `Service/IInvoicePdfService.cs`, `JavaInvoicePdfService.cs`, `JavaMicroserviceClient.cs` | `src/adapter.mjs` | `test/adapter.test.mjs` | A01–A07 (7) |
| State | Maintenance Schedule Workflow (CH03): `Service/Maintenance/*.cs` | `src/state.mjs` | `test/state.test.mjs` with `fixtures/state_cases.txt` | T01–T20 (20) |

All source paths are relative to `WanderCar_DotNet/src/FlemanApi/`.

## Requirements

- Node.js **22 or later**. It was tested with **v22.18.0**.
- No npm packages. The tests use Node's built-in `node:test` runner and `node:assert/strict`.
- The code uses ES modules (`.mjs`), ES2022 `#private` class fields, async generators and `AbortController`.

## Run the tests

From `Part3/javascript`:

```bash
node --test                                  # all 59 tests (same as: npm test)
node --test test/dependency_injection.test.mjs   # one pattern
```

**Expected result:** `# tests 59`, `# pass 59`, `# fail 0`.

**Logs:**

| Log | Contents |
|---|---|
| [`../logs/javascript_test.log`](../logs/javascript_test.log) | Full suite |
| [`../logs/javascript_dependency_injection_test.log`](../logs/javascript_dependency_injection_test.log) | DI only |
| [`../logs/javascript_mutation_check.log`](../logs/javascript_mutation_check.log) | One deliberate bug per pattern; all are caught |
| [`../logs/timing.log`](../logs/timing.log) | Median process time over 11 runs |

## Dependency Injection: how it is used

```js
import { Lifetime, ServiceCollection } from './src/dependency_injection.mjs';

const provider = new ServiceCollection()
  .addScoped('FlemanDbContext', InMemoryDbContext)
  .addOpenGeneric('IGenericRepository', Lifetime.SCOPED,
    (entity, sp) => new GenericRepository(sp.getRequiredService('FlemanDbContext'), entity, 'id'))
  .addSingleton('TimeProvider', systemClock)              // an instance: used as-is
  .addScoped('IMaintenanceService', MaintenanceService,   // a class: constructor-injected
    ['IGenericRepository<MaintenanceSchedule>', 'IGenericRepository<Car>', 'TimeProvider'])
  .build();

const scope = provider.createScope();                     // one scope per "request"
const service = scope.getRequiredService('IMaintenanceService');
```

The table compares this with the C# original:

| C# (`Program.cs`) | JavaScript |
|---|---|
| `AddScoped<IX, X>()` | `addScoped('IX', X, [deps])`. JavaScript has no constructor parameter types, so dependencies are named explicitly. |
| `AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>))` | `addOpenGeneric('IGenericRepository', Lifetime.SCOPED, factory)`, resolved by parsing the token `IGenericRepository<Car>` |
| `AddSingleton(TimeProvider.System)` | `addSingleton('TimeProvider', instance)`. A non-function value is used as the instance. |
| `CreateScope()` / `GetRequiredService<T>()` | `createScope()` / `getRequiredService('T')` |
| `InvalidOperationException` messages | The same message text in `Error` |

## Language notes

- **Interfaces become duck typing.** C# interfaces such as `IGenericRepository` and `IInvoicePdfService` have no JavaScript equivalent. A role exists only as a method shape, which test A07 relies on.
- **Generics become constructor arguments.** `GenericRepository<TEntity, TKey>` becomes `new GenericRepository(context, entityName, keyName)`.
- **Async:**
  - `Task` becomes `Promise`.
  - `Channel<T>` and `IAsyncEnumerable` become an async generator consumed with `for await`.
  - `CancellationToken` becomes `AbortSignal`.
  - Node is single-threaded, so the queue needs no locking.
- **Encapsulation:** `private readonly` becomes `#private` fields, and enums become `Object.freeze` objects.
