# CH04 prompt - Booking lifecycle notifications (Observer)

Owner: Vani Sugovind S R. Paste everything below the line into your LLM, and attach the listed source files.

**Files to attach from `WanderCar_DotNet/src/`:** `FlemanApi/Service/BookingService.cs`, `FlemanApi/Service/IBookingService.cs`, `FlemanApi/Service/IEmailService.cs`, `FlemanApi/Service/EmailService.cs`, `FlemanApi/Service/EmailBackgroundQueue.cs`, `FlemanApi/Service/EmailQueueHostedService.cs`, `FlemanApi/Models/BookingHeader.cs`, `FlemanApi/Program.cs`, `FlemanApi.Tests/Services/BookingServiceTests.cs`, `FlemanApi.Tests/Services/EmailServiceTests.cs`, `FlemanApi.Tests/TestHelpers/*.cs`

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


## The change: CH04 - Booking lifecycle notifications

**Baseline behaviour.** `BookingService.CreateBookingAsync` builds the confirmation e-mail text inline and calls `IEmailService.SendEmailAsync` directly inside a try/catch. `ModifyBookingAsync` and `CancelBookingAsync` notify nobody. E-mail is queued through `EmailBackgroundQueue` and sent by `EmailQueueHostedService`.

**Requirement.** Customers are notified when a booking is created, modified or cancelled, and each event is written to an audit log. Use the **Observer** pattern so new reactions can be added without editing `BookingService`.

**Design to implement.**
- Event type: a small record `BookingEvent` carrying the event kind (CREATED, MODIFIED, CANCELLED), the `BookingHeader` data needed for the message, the customer name and e-mail, and the car type name.
- `IBookingEventObserver` (Observer) with one async method that receives a `BookingEvent`.
- `IBookingEventPublisher` (Subject) with `Subscribe`, `Unsubscribe` and `PublishAsync`; `BookingEventPublisher` (ConcreteSubject) keeps the observer list and notifies each observer in turn. One observer failing must be logged and must not stop the others or fail the booking request, matching the baseline's swallow-and-log behaviour.
- `EmailBookingObserver` (ConcreteObserver): for CREATED it sends exactly the baseline subject and body through `IEmailService.SendEmailAsync`; for MODIFIED and CANCELLED it sends new messages.
- `AuditLogBookingObserver` (ConcreteObserver): writes one structured `ILogger` entry per event.
- Observers are registered in `Program.cs` and subscribed when the publisher is constructed (inject `IEnumerable<IBookingEventObserver>`).
- `BookingService` receives `IBookingEventPublisher`, publishes after each successful save, and no longer builds e-mail text. Decide with me whether the `IEmailService` constructor parameter is removed from `BookingService`.
- Do not touch `StaffService`, `CustomerService` or the e-mail queue classes. No database change and no migration.

**Acceptance criteria.**
1. Creating a booking still queues one confirmation e-mail with the same recipient, subject and body as the baseline.
2. Modifying and cancelling a booking each queue one e-mail and write one audit entry.
3. An observer that throws does not change the HTTP result and the remaining observers still run.
4. All existing tests pass; the only edit to `BookingServiceTests` is the constructor argument. If an existing test verifies the direct `SendEmailAsync` call, keep the assertion true by wiring the real `EmailBookingObserver` to the mocked `IEmailService` in that test's `SetUp`, and tell me exactly what you changed.
5. New tests cover the publisher (subscribe, unsubscribe, ordering, failure isolation) and each observer.

**Refactoring to document.** Code smell: `CreateBookingAsync` is a long method with mixed responsibilities (booking rules plus message formatting). Technique: Extract Class / Move Method of the notification code into `EmailBookingObserver`. Behaviour-preservation evidence: the existing booking tests plus a test asserting the CREATED message text equals the baseline text.

**Pattern effects to expect.** Newly introduced: Observer, "Booking Lifecycle Notification". The e-mail queue instances (Command, Producer-Consumer, SMTP Adapter) are preserved and gain a new client. Dependency Injection is extended.
