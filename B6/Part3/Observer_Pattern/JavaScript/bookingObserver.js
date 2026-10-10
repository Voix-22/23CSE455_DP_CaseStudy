// Observer pattern - Booking Lifecycle Notification (port of CH04, C# FlemanApi/Service/Notifications).
'use strict';

const BookingEventKind = Object.freeze({ CREATED: 'CREATED', MODIFIED: 'MODIFIED', CANCELLED: 'CANCELLED' });

// Push-model event: the subject sends all the state the observers need.
function bookingEvent(kind, confirmationNo, customerName, customerEmail, carTypeName, estimatedAmount) {
  return Object.freeze({ kind, confirmationNo, customerName, customerEmail, carTypeName, estimatedAmount });
}

// Observer (no interface construct: any object with onBookingEvent(event) is an observer).
class BookingEventObserver {
  onBookingEvent(_event) { throw new Error('not implemented'); }
}

// Subject
class BookingEventSubject {
  subscribe(_observer) { throw new Error('not implemented'); }
  unsubscribe(_observer) { throw new Error('not implemented'); }
  publish(_event) { throw new Error('not implemented'); }
}

// ConcreteSubject: notifies in subscription order; one failing observer is
// recorded as a warning and does not stop the others.
class BookingEventPublisher extends BookingEventSubject {
  #observers = [];
  #warnings = [];

  constructor(observers = []) {
    super();
    for (const observer of observers) this.subscribe(observer);
  }

  subscribe(observer) {
    // includes() compares by identity for objects: the same object is subscribed once.
    if (!this.#observers.includes(observer)) this.#observers.push(observer);
  }

  unsubscribe(observer) {
    this.#observers = this.#observers.filter((existing) => existing !== observer);
  }

  publish(event) {
    for (const observer of [...this.#observers]) {
      try {
        observer.onBookingEvent(event);
      } catch (err) {
        this.#warnings.push(`observer failed for ${event.kind} ${event.confirmationNo}: ${err.message}`);
      }
    }
  }

  warnings() { return this.#warnings; }
}

const SUBJECTS = Object.freeze({
  CREATED: ['Booking Confirmation - Fleeman', 'confirmed'],
  MODIFIED: ['Booking Updated - Fleeman', 'updated'],
  CANCELLED: ['Booking Cancelled - Fleeman', 'cancelled'],
});

// ConcreteObserver: e-mails the customer.
class EmailBookingObserver extends BookingEventObserver {
  #outbox;

  constructor(outbox) { super(); this.#outbox = outbox; }

  onBookingEvent(e) {
    if (!e.customerEmail) return;
    const [subject, verb] = SUBJECTS[e.kind];
    const body = `Dear ${e.customerName}, your booking ${e.confirmationNo} (${e.carTypeName}) has been ${verb}.`;
    this.#outbox.push({ to: e.customerEmail, subject, body });
  }
}

// ConcreteObserver: one audit line per event.
class AuditLogBookingObserver extends BookingEventObserver {
  #auditLog;

  constructor(auditLog) { super(); this.#auditLog = auditLog; }

  onBookingEvent(e) {
    this.#auditLog.push(`AUDIT ${e.kind} ${e.confirmationNo} amount=${e.estimatedAmount.toFixed(2)}`);
  }
}

module.exports = {
  BookingEventKind, bookingEvent, BookingEventObserver, BookingEventSubject,
  BookingEventPublisher, EmailBookingObserver, AuditLogBookingObserver,
};
