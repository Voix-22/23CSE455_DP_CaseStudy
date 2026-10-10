# Observer in Python

This folder is the Python port of the **Observer** pattern instance **"Booking Lifecycle Notification"**, introduced by CH04 in the WanderCar / Fleman .NET backend (`FlemanApi/Service/Notifications/`, commit `9be38a6f`). It implements [`../common_spec.md`](../common_spec.md) and nothing else.

## Files

```
Python/
├── booking_observer.py   the pattern (snippet measured for LOC)
├── test_runner.py        the 8 cases of common_spec.md, no framework
├── test_result.txt       real output of the logged run
└── timings.txt           raw timings from ../measure.py
```

## Participants

| Role | Python | C# original |
|---|---|---|
| Subject | `BookingEventSubject(ABC)` | `IBookingEventPublisher` |
| ConcreteSubject | `BookingEventPublisher` | `BookingEventPublisher` |
| Observer | `BookingEventObserver(ABC)` with `on_booking_event` | `IBookingEventObserver` |
| ConcreteObserver | `EmailBookingObserver`, `AuditLogBookingObserver` | the same names |
| Event | `@dataclass(frozen=True) BookingEvent`, `Enum BookingEventKind` | `record BookingEvent`, `enum BookingEventKind` |

## Tool, working folder and commands

- Tool: **CPython 3.14.6**. Any Python 3.10 or later works, because the code uses `str | None`. Standard library only.
- Working folder: `Part3/Observer_Pattern/Python`

```bash
python test_runner.py
```

**Expected result:** eight `PASS` lines, then `PASS 8/8`, with exit code 0. The real output is in [`test_result.txt`](test_result.txt).

## Python-specific notes

- **The abstractions are abstract base classes** (`abc.ABC` with `@abstractmethod`). They are enforced when an object is constructed, not at compile time. `publish` would accept any object with an `on_booking_event` method (duck typing); the ABC only documents the role.
- **The subject and verb are looked up in a dict** keyed by the enum member, with no `if`/`match` chain. That keeps the cyclomatic complexity of the e-mail observer at 2.
- **Identity:** `subscribe` uses `is`, not `==`. A frozen dataclass observer would compare by value under `==`.
- **The event is immutable:** `frozen=True` makes the `BookingEvent` read-only, like the C# record.
- **Build outputs:** running the tests creates `__pycache__/`. `../.gitignore` excludes it from the upload.
