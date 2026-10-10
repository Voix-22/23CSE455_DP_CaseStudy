# Observer pattern - Booking Lifecycle Notification (port of CH04, C# FlemanApi/Service/Notifications).
from abc import ABC, abstractmethod
from dataclasses import dataclass
from enum import Enum


class BookingEventKind(Enum):
    CREATED = "CREATED"
    MODIFIED = "MODIFIED"
    CANCELLED = "CANCELLED"


# Push-model event: the subject sends all the state the observers need.
@dataclass(frozen=True)
class BookingEvent:
    kind: BookingEventKind
    confirmation_no: str
    customer_name: str
    customer_email: str | None
    car_type_name: str
    estimated_amount: float


@dataclass(frozen=True)
class Email:
    to: str
    subject: str
    body: str


# Observer
class BookingEventObserver(ABC):
    @abstractmethod
    def on_booking_event(self, event: BookingEvent) -> None: ...


# Subject
class BookingEventSubject(ABC):
    @abstractmethod
    def subscribe(self, observer: BookingEventObserver) -> None: ...

    @abstractmethod
    def unsubscribe(self, observer: BookingEventObserver) -> None: ...

    @abstractmethod
    def publish(self, event: BookingEvent) -> None: ...


# ConcreteSubject: notifies in subscription order; one failing observer is
# recorded as a warning and does not stop the others.
class BookingEventPublisher(BookingEventSubject):
    def __init__(self, observers=()):
        self._observers: list[BookingEventObserver] = []
        self._warnings: list[str] = []
        for observer in observers:
            self.subscribe(observer)

    def subscribe(self, observer):
        # Identity check, not ==: the same object is subscribed once.
        if not any(existing is observer for existing in self._observers):
            self._observers.append(observer)

    def unsubscribe(self, observer):
        self._observers = [existing for existing in self._observers if existing is not observer]

    def publish(self, event):
        for observer in list(self._observers):
            try:
                observer.on_booking_event(event)
            except Exception as ex:
                self._warnings.append(f"observer failed for {event.kind.value} {event.confirmation_no}: {ex}")

    def warnings(self):
        return self._warnings


_SUBJECTS = {
    BookingEventKind.CREATED: ("Booking Confirmation - Fleeman", "confirmed"),
    BookingEventKind.MODIFIED: ("Booking Updated - Fleeman", "updated"),
    BookingEventKind.CANCELLED: ("Booking Cancelled - Fleeman", "cancelled"),
}


# ConcreteObserver: e-mails the customer.
class EmailBookingObserver(BookingEventObserver):
    def __init__(self, outbox: list[Email]):
        self._outbox = outbox

    def on_booking_event(self, event):
        if not event.customer_email:
            return
        subject, verb = _SUBJECTS[event.kind]
        body = (f"Dear {event.customer_name}, your booking {event.confirmation_no} "
                f"({event.car_type_name}) has been {verb}.")
        self._outbox.append(Email(event.customer_email, subject, body))


# ConcreteObserver: one audit line per event.
class AuditLogBookingObserver(BookingEventObserver):
    def __init__(self, audit_log: list[str]):
        self._audit_log = audit_log

    def on_booking_event(self, event):
        self._audit_log.append(f"AUDIT {event.kind.value} {event.confirmation_no} amount={event.estimated_amount:.2f}")
