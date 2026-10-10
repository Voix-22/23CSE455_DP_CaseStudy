# Common specification - Observer: Booking Lifecycle Notification

Source instance: the Observer introduced by CH04 in the C# case study (`WanderCar_DotNet/src/FlemanApi/Service/Notifications/`, commit `9be38a6ff4cd5b10b813aedb42c3de8a1e82730d`, see `Part2/changes/Change4/`). Every language folder implements exactly this specification and nothing else.

The C# version is asynchronous (`Task`) and sends e-mail through `IEmailService` and writes the audit entry through `ILogger`. Here notification is **synchronous**, and both side effects are reduced to appending to an in-memory list (an *outbox* and an *audit log*). This keeps the four ports comparable. The pattern structure is unchanged.

## Participants

| Role | Name | Members | C# original |
|---|---|---|---|
| Enumeration | `BookingEventKind` | `CREATED`, `MODIFIED`, `CANCELLED` | `BookingEventKind` |
| Event (push-model state) | `BookingEvent` | `kind`, `confirmationNo`, `customerName`, `customerEmail` (optional), `carTypeName`, `estimatedAmount` | `BookingEvent` record |
| Observer (abstraction) | `BookingEventObserver` | `onBookingEvent(event)` | `IBookingEventObserver` |
| Subject (abstraction) | `BookingEventSubject` | `subscribe(observer)`, `unsubscribe(observer)`, `publish(event)` | `IBookingEventPublisher` |
| ConcreteSubject | `BookingEventPublisher` | constructor takes the initial observers; `warnings()` | `BookingEventPublisher` |
| ConcreteObserver | `EmailBookingObserver` | constructor takes the outbox | `EmailBookingObserver` |
| ConcreteObserver | `AuditLogBookingObserver` | constructor takes the audit log | `AuditLogBookingObserver` |

Names follow each language's naming convention (for example `on_booking_event` in Python).

## Rules

**BookingEventPublisher**
- The constructor subscribes the given observers in the given order (the C# constructor receives `IEnumerable<IBookingEventObserver>` from DI).
- `subscribe` appends an observer. Subscribing the same object twice has no effect (identity, not equality).
- `unsubscribe` removes an observer. Removing one that is not subscribed has no effect.
- `publish` notifies every subscribed observer once, in subscription order, iterating over a copy of the list.
- If an observer throws, `publish` catches it, appends exactly one warning `observer failed for <KIND> <confirmationNo>: <error message>`, and continues with the next observer. `publish` itself never throws.

**EmailBookingObserver** appends one e-mail `(to, subject, body)` to its outbox:

| Kind | Subject | Body |
|---|---|---|
| CREATED | `Booking Confirmation - Fleeman` | `Dear <customerName>, your booking <confirmationNo> (<carTypeName>) has been confirmed.` |
| MODIFIED | `Booking Updated - Fleeman` | `Dear <customerName>, your booking <confirmationNo> (<carTypeName>) has been updated.` |
| CANCELLED | `Booking Cancelled - Fleeman` | `Dear <customerName>, your booking <confirmationNo> (<carTypeName>) has been cancelled.` |

If `customerEmail` is absent or empty it appends nothing. This matches the baseline, where no e-mail was sent when the customer row was missing.

The subjects are the C# subjects. The bodies are shortened to one line, so the comparison is about the pattern, not about string templates.

**AuditLogBookingObserver** appends one line per event: `AUDIT <KIND> <confirmationNo> amount=<estimatedAmount with 2 decimals>`, for example `AUDIT CREATED ABC12345 amount=500.00`. The decimal point is always `.`, whatever the locale.

## Test fixture

Unless a case says otherwise, the event is `kind`, `ABC12345`, `Jane Doe`, `jane@example.com`, `Sedan`, `500.0`.

A *recording observer* named `X` appends `X:<KIND>` to a shared call list. A *throwing observer* throws an error with the message `SMTP down`.

## Test cases (identical in all four languages)

| # | Setup and input | Expected |
|---|---|---|
| 1 | Publisher with [email, audit]; publish CREATED | outbox is exactly one e-mail: `jane@example.com`, `Booking Confirmation - Fleeman`, `Dear Jane Doe, your booking ABC12345 (Sedan) has been confirmed.`; audit log is exactly `AUDIT CREATED ABC12345 amount=500.00` |
| 2 | Same; publish MODIFIED | one e-mail with subject `Booking Updated - Fleeman` and body ending `has been updated.`; audit log is exactly `AUDIT MODIFIED ABC12345 amount=500.00` |
| 3 | Same; publish CANCELLED | one e-mail with subject `Booking Cancelled - Fleeman` and body ending `has been cancelled.`; audit log is exactly `AUDIT CANCELLED ABC12345 amount=500.00` |
| 4 | Publisher constructed with recording observers [A, B, C]; publish CREATED | calls are `A:CREATED`, `B:CREATED`, `C:CREATED`, in that order |
| 5 | Publisher with [A]; `subscribe(A)` again; publish CREATED | calls are exactly `A:CREATED` |
| 6 | Publisher with [A, B, C]; `unsubscribe(B)`; publish CANCELLED | calls are `A:CANCELLED`, `C:CANCELLED` |
| 7 | Publisher with [A, throwing, C]; publish CREATED | `publish` does not throw; calls are `A:CREATED`, `C:CREATED`; warnings are exactly `observer failed for CREATED ABC12345: SMTP down` |
| 8 | Publisher with [email, audit]; publish CREATED with no customer e-mail | outbox is empty; audit log has exactly one line |

## Test runner contract

No test framework. One line per case, `PASS <n> <description>` or `FAIL <n> <description>`, then a final line `PASS <passed>/8`. Exit code 0 only if all eight pass, otherwise 1.
