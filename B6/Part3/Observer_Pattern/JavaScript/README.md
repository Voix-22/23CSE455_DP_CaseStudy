# Observer in JavaScript

This folder is the JavaScript port of the **Observer** pattern instance **"Booking Lifecycle Notification"**, introduced by CH04 in the WanderCar / Fleman .NET backend (`FlemanApi/Service/Notifications/`, commit `9be38a6f`). It implements [`../common_spec.md`](../common_spec.md) and nothing else.

## Files

```
JavaScript/
├── bookingObserver.js   the pattern (snippet measured for LOC), CommonJS module
├── testRunner.js        the 8 cases of common_spec.md, no framework
├── test_result.txt      real output of the logged run
└── timings.txt          raw timings from ../measure.py
```

## Participants

| Role | JavaScript | C# original |
|---|---|---|
| Subject | `class BookingEventSubject` (its methods throw `not implemented`) | `IBookingEventPublisher` |
| ConcreteSubject | `class BookingEventPublisher extends BookingEventSubject` | `BookingEventPublisher` |
| Observer | `class BookingEventObserver`, but in practice any object with `onBookingEvent(event)` | `IBookingEventObserver` |
| ConcreteObserver | `EmailBookingObserver`, `AuditLogBookingObserver` | the same names |
| Event | `bookingEvent(...)` factory returning a frozen object; `BookingEventKind` is a frozen object of strings | `record BookingEvent`, `enum BookingEventKind` |

## Tool, working folder and commands

- Tool: **Node.js v24.18.0**. Any Node.js 14 or later works, because the code uses `#private` fields and optional catch binding. No npm packages.
- Working folder: `Part3/Observer_Pattern/JavaScript`

```bash
node testRunner.js
```

**Expected result:** eight `PASS` lines, then `PASS 8/8`, with exit code 0. The real output is in [`test_result.txt`](test_result.txt).

## JavaScript-specific notes

- **No interface construct.** The abstractions are documented by base classes whose methods throw. `publish` really relies on duck typing, and the tests pass plain object literals `{ onBookingEvent }` as observers.
- **Identity:** `Array.prototype.includes` and `!==` compare objects by reference, which is exactly the identity rule in the spec.
- **Encapsulation:** the observer list, warnings, outbox and audit log are `#private` fields, like `private readonly` in C#.
- **Synchronous by design.** A real Node implementation would usually `await` each observer, or use the built-in `EventEmitter`. `EventEmitter` was not used because it does not isolate a throwing listener: an exception stops the remaining listeners, which breaks spec case 7.
- **Number formatting:** `toFixed(2)` always uses `.`, so there is no locale issue.
