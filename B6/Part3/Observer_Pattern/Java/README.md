# Observer in Java

This folder is the Java port of the **Observer** pattern instance **"Booking Lifecycle Notification"**, introduced by CH04 in the WanderCar / Fleman .NET backend (`FlemanApi/Service/Notifications/`, commit `9be38a6f`). It implements [`../common_spec.md`](../common_spec.md) and nothing else.

## Files

```
Java/
├── BookingEventPublisher.java   the pattern (snippet measured for LOC)
├── TestRunner.java              the 8 cases of common_spec.md, no framework
├── test_result.txt              real output of the logged run
└── timings.txt                  raw timings from ../measure.py
```

## Participants

| Role | Java | C# original |
|---|---|---|
| Subject | `interface BookingEventSubject` | `IBookingEventPublisher` |
| ConcreteSubject | `public class BookingEventPublisher` | `BookingEventPublisher` |
| Observer | `interface BookingEventObserver` (a functional interface, so a lambda is an observer) | `IBookingEventObserver` |
| ConcreteObserver | `final class EmailBookingObserver`, `final class AuditLogBookingObserver` | the same names |
| Event | `record BookingEvent`, `enum BookingEventKind` | `record BookingEvent`, `enum BookingEventKind` |

## Tool, working folder and commands

- Tool: **javac 26.0.1 / Java HotSpot 64-Bit Server VM 26.0.1+8-34**. Any JDK 17 or later works, because the code uses records and switch expressions.
- Working folder: `Part3/Observer_Pattern/Java`

```bash
javac BookingEventPublisher.java TestRunner.java
java -cp . TestRunner
```

**Expected result:** eight `PASS` lines, then `PASS 8/8`, with exit code 0. The real output is in [`test_result.txt`](test_result.txt).

## Java-specific notes

- **One snippet file:** Java allows only one public top-level class per file. The other participants are package-private top-level types in the same file, so `TestRunner` (same default package) can use them.
- **Interfaces are nominal and checked at compile time.** `BookingEventObserver` has a single abstract method, which makes it a functional interface. The test's recording and throwing observers are therefore lambdas.
- **Identity:** `subscribe` and `unsubscribe` compare with `==`, not `equals()`. A record's `equals()` is value equality, so this matters for record observers.
- **Exhaustive `switch`:** the `switch` expressions over the enum have no `default`. The compiler rejects them if a `BookingEventKind` constant is added and not handled.
- **Locale:** `String.format(Locale.ROOT, "%.2f", ...)` keeps the decimal point as `.` on every locale.
- **Build outputs:** compiling writes nine `.class` files next to the sources. `../.gitignore` excludes them from the upload.
