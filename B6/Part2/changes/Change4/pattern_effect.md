# Change 4: Pattern effects

All paths are relative to `WanderCar_DotNet/src/`. New-code line numbers refer to `ch04` (`9be38a6f`), which is one commit on top of `baseline` (`dfc61ab8`) and is not rebased onto `ch03`. Baseline line numbers refer to `dfc61ab8`.

> Only the pattern instances that CH04 touches or that the CH04 brief names are listed individually below. Any other baseline instance in the team catalogue should be marked **preserved**, with the reason "no file of this instance is changed by CH04 (see `changed_files.txt`)". Examples are the Java Microservice Adapter, the Legacy API Proxy, the Strangler Fig and the Request Middleware Chain.

## Newly introduced

### Observer: "Booking Lifecycle Notification"

`BookingService` publishes one `BookingEvent` after each successful create, modify or cancel. It neither knows nor names the reactions. Adding a reaction means adding one `IBookingEventObserver` class and one `Program.cs` line, with no edit to `BookingService`.

| Role | Class / method | File:line |
|---|---|---|
| Subject (interface) | `IBookingEventPublisher` with `Subscribe`, `Unsubscribe` and `PublishAsync` | `FlemanApi/Service/Notifications/IBookingEventPublisher.cs:4` |
| ConcreteSubject | `BookingEventPublisher`, which keeps `List<IBookingEventObserver>` | `FlemanApi/Service/Notifications/BookingEventPublisher.cs:10` |
| Attach | `BookingEventPublisher.Subscribe`, which ignores duplicates | `FlemanApi/Service/Notifications/BookingEventPublisher.cs:21` |
| Attach at construction | constructor subscribes each injected `IEnumerable<IBookingEventObserver>` in DI order | `FlemanApi/Service/Notifications/BookingEventPublisher.cs:15-19` |
| Detach | `BookingEventPublisher.Unsubscribe` | `FlemanApi/Service/Notifications/BookingEventPublisher.cs:26` |
| Notify | `BookingEventPublisher.PublishAsync`, which loops over a copy of the list (line 32) and isolates each observer in a try/catch (lines 34-43) | `FlemanApi/Service/Notifications/BookingEventPublisher.cs:28` |
| Observer (interface) | `IBookingEventObserver.OnBookingEventAsync(BookingEvent)` | `FlemanApi/Service/Notifications/IBookingEventObserver.cs:6-8` |
| ConcreteObserver | `EmailBookingObserver`, which selects subject and body by `Kind` (line 24) and sends through `IEmailService.SendEmailAsync` (line 32) | `FlemanApi/Service/Notifications/EmailBookingObserver.cs:6` |
| ConcreteObserver | `AuditLogBookingObserver`, which writes one structured `LogInformation` entry (line 18) | `FlemanApi/Service/Notifications/AuditLogBookingObserver.cs:7` |
| Event (push-model state) | `BookingEvent` record and `BookingEventKind` enum | `FlemanApi/Service/Notifications/BookingEvent.cs:5`, `:9` |
| Client that raises events | `BookingService.PublishBookingEventAsync`, called at lines 168 (CREATED), 292 (MODIFIED) and 316 (CANCELLED) | `FlemanApi/Service/BookingService.cs:338` |
| Wiring | CH04 registration block | `FlemanApi/Program.cs:211-216` |

Variant: this is the **push** model. The subject sends the whole state the observers need inside `BookingEvent`, so observers never call back into `BookingService`. The subject is not the domain object itself. `BookingService` delegates to a separate publisher, which some texts call an *event aggregator* or *mediator-style* Observer.

## Modified

### Service Layer: "Booking Service Layer"

- **Effect:** modified. The public interface `IBookingService` is unchanged. The implementation `BookingService` changes in three ways:
  - The constructor parameter `IEmailService emailService` (baseline `Service/BookingService.cs:57`) is replaced by `IBookingEventPublisher bookingEvents` (`:58`).
  - The inline e-mail block (baseline `:167-196`, 30 lines) is replaced by one call at `:168`.
  - Publish calls are added after the save in `ModifyBookingAsync` (`:292`) and `CancelBookingAsync` (`:316`), with the private helper `PublishBookingEventAsync` (`:338-361`).
- **Reason:** message formatting and delivery moved out of the service. The service now owns booking rules only (see `refactoring.md`).

## Extended

### Dependency Injection: "ASP.NET Core DI Composition"

- **Effect:** extended. These registrations are new, in one CH04 block (`FlemanApi/Program.cs:211-216`):
  - two `IBookingEventObserver` registrations, `EmailBookingObserver` and `AuditLogBookingObserver` (`:214`, `:215`)
  - `IBookingEventPublisher` → `BookingEventPublisher` (`:216`)
- **New DI feature in this code base:** multiple implementations of one service type, resolved as `IEnumerable<IBookingEventObserver>` in the `BookingEventPublisher` constructor (`BookingEventPublisher.cs:15`). Registration order defines notification order.
- **Changed constructor injection:** `BookingService` now receives `IBookingEventPublisher` (`BookingService.cs:58`). `EmailBookingObserver` receives `IEmailService` (`EmailBookingObserver.cs:14`). `AuditLogBookingObserver` and `BookingEventPublisher` receive an `ILogger<T>`.
- No existing registration line is edited.

## Preserved (gain a new client)

### Command: "Queued E-mail Work Item"

- **Effect:** preserved.
- **Participants:**
  - Each e-mail is a `Func<IEmailSender, CancellationToken, Task>` work item created in `EmailService.SendEmailAsync` (`FlemanApi/Service/EmailService.cs:17-21`).
  - The work item is executed later by `EmailQueueHostedService` (`FlemanApi/Service/EmailQueueHostedService.cs:25`).
- **New client:** `EmailBookingObserver` → `IEmailService.SendEmailAsync`. It creates the same kind of work item for CREATED, MODIFIED and CANCELLED. Before CH04 only CREATED did.

### Producer-Consumer: "Email Background Work Queue"

- **Effect:** preserved. `EmailBackgroundQueue` (`FlemanApi/Service/EmailBackgroundQueue.cs:15-25`, a bounded channel of 100) and `EmailQueueHostedService` (`FlemanApi/Service/EmailQueueHostedService.cs:19-32`) are not edited.
- **What changes:** the producer side now has one more caller, `EmailBookingObserver` via `EmailService`. Booking traffic now produces up to three kinds of message instead of one.
- **Evidence:** `BookingServiceNotificationTests` runs the real `EmailService` against a mocked `IEmailBackgroundQueue` and asserts one `Enqueue` per event.

### Adapter: "SMTP Adapter"

- **Effect:** preserved. `MailKitEmailSender` adapts MailKit to `IEmailSender` (`FlemanApi/Service/MailKitEmailSender.cs:9`, `SendPlainAsync` at `:20`) and is not edited. It sends the new MODIFIED and CANCELLED messages with no change.

### Repository: "Generic Repository"

- **Effect:** preserved. No new entity and no new client class. `BookingService` already used `IGenericRepository<Customer,long>` and `IGenericRepository<CarType,long>`. It now also calls them from `PublishBookingEventAsync` (`BookingService.cs:342-343`).

### Global exception handling (middleware)

- **Effect:** preserved. `ExceptionHandlingMiddleware` is not edited. Notification failures never reach it, because they are caught in `BookingEventPublisher.PublishAsync` and in `BookingService.PublishBookingEventAsync`. This is the same as the baseline try/catch, so the HTTP result of create, modify and cancel is unchanged.

## Not affected

`CustomerService` (the welcome e-mail, `Service/CustomerService.cs:118`) and `StaffService` (the return invoice e-mail, `Service/StaffService.cs:213`) still call `IEmailService` directly. They were left out of the Observer on purpose, as the CH04 brief requires. They are candidates for the same treatment in a later change.
