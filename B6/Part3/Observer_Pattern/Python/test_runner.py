# Runs the 8 cases of ../common_spec.md. No test framework.
import sys

from booking_observer import (
    AuditLogBookingObserver,
    BookingEvent,
    BookingEventKind,
    BookingEventObserver,
    BookingEventPublisher,
    Email,
    EmailBookingObserver,
)

passed = 0


def check(n, description, test):
    global passed
    try:
        ok = bool(test())
    except Exception:
        ok = False
    print(f"{'PASS' if ok else 'FAIL'} {n} {description}")
    if ok:
        passed += 1


def event(kind, email="jane@example.com"):
    return BookingEvent(kind, "ABC12345", "Jane Doe", email, "Sedan", 500.0)


class Recorder(BookingEventObserver):
    def __init__(self, name, calls):
        self.name, self.calls = name, calls

    def on_booking_event(self, e):
        self.calls.append(f"{self.name}:{e.kind.value}")


class Thrower(BookingEventObserver):
    def on_booking_event(self, e):
        raise RuntimeError("SMTP down")


def email_and_audit(kind, subject, body_suffix):
    outbox, audit = [], []
    BookingEventPublisher([EmailBookingObserver(outbox), AuditLogBookingObserver(audit)]).publish(event(kind))
    return (len(outbox) == 1
            and outbox[0].to == "jane@example.com"
            and outbox[0].subject == subject
            and outbox[0].body.endswith(body_suffix)
            and audit == [f"AUDIT {kind.value} ABC12345 amount=500.00"])


def case1():
    outbox, audit = [], []
    BookingEventPublisher([EmailBookingObserver(outbox), AuditLogBookingObserver(audit)]).publish(event(BookingEventKind.CREATED))
    return (outbox == [Email("jane@example.com", "Booking Confirmation - Fleeman",
                             "Dear Jane Doe, your booking ABC12345 (Sedan) has been confirmed.")]
            and audit == ["AUDIT CREATED ABC12345 amount=500.00"])


def case4():
    calls = []
    BookingEventPublisher([Recorder("A", calls), Recorder("B", calls), Recorder("C", calls)]).publish(event(BookingEventKind.CREATED))
    return calls == ["A:CREATED", "B:CREATED", "C:CREATED"]


def case5():
    calls = []
    a = Recorder("A", calls)
    publisher = BookingEventPublisher([a])
    publisher.subscribe(a)
    publisher.publish(event(BookingEventKind.CREATED))
    return calls == ["A:CREATED"]


def case6():
    calls = []
    b = Recorder("B", calls)
    publisher = BookingEventPublisher([Recorder("A", calls), b, Recorder("C", calls)])
    publisher.unsubscribe(b)
    publisher.publish(event(BookingEventKind.CANCELLED))
    return calls == ["A:CANCELLED", "C:CANCELLED"]


def case7():
    calls = []
    publisher = BookingEventPublisher([Recorder("A", calls), Thrower(), Recorder("C", calls)])
    publisher.publish(event(BookingEventKind.CREATED))
    return (calls == ["A:CREATED", "C:CREATED"]
            and publisher.warnings() == ["observer failed for CREATED ABC12345: SMTP down"])


def case8():
    outbox, audit = [], []
    BookingEventPublisher([EmailBookingObserver(outbox), AuditLogBookingObserver(audit)]).publish(event(BookingEventKind.CREATED, None))
    return outbox == [] and len(audit) == 1


check(1, "CREATED sends the confirmation e-mail and writes one audit line", case1)
check(2, "MODIFIED sends the update e-mail and writes one audit line",
      lambda: email_and_audit(BookingEventKind.MODIFIED, "Booking Updated - Fleeman", "has been updated."))
check(3, "CANCELLED sends the cancellation e-mail and writes one audit line",
      lambda: email_and_audit(BookingEventKind.CANCELLED, "Booking Cancelled - Fleeman", "has been cancelled."))
check(4, "observers are notified in subscription order", case4)
check(5, "subscribing the same observer twice notifies it once", case5)
check(6, "an unsubscribed observer is no longer notified", case6)
check(7, "a throwing observer is logged and the others still run", case7)
check(8, "no customer e-mail: nothing sent, audit still written", case8)
print(f"PASS {passed}/8")
sys.exit(0 if passed == 8 else 1)
