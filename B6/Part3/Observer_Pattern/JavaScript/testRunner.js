// Runs the 8 cases of ../common_spec.md. No test framework.
'use strict';

const {
  BookingEventKind, bookingEvent, BookingEventPublisher, EmailBookingObserver, AuditLogBookingObserver,
} = require('./bookingObserver.js');

let passed = 0;

function check(n, description, test) {
  let ok;
  try {
    ok = Boolean(test());
  } catch {
    ok = false;
  }
  console.log(`${ok ? 'PASS' : 'FAIL'} ${n} ${description}`);
  if (ok) passed++;
}

const same = (a, b) => JSON.stringify(a) === JSON.stringify(b);
const event = (kind, email = 'jane@example.com') => bookingEvent(kind, 'ABC12345', 'Jane Doe', email, 'Sedan', 500.0);
const recorder = (name, calls) => ({ onBookingEvent: (e) => calls.push(`${name}:${e.kind}`) });

function emailAndAudit(kind, subject, bodySuffix) {
  const outbox = [];
  const audit = [];
  new BookingEventPublisher([new EmailBookingObserver(outbox), new AuditLogBookingObserver(audit)]).publish(event(kind));
  return outbox.length === 1
    && outbox[0].to === 'jane@example.com'
    && outbox[0].subject === subject
    && outbox[0].body.endsWith(bodySuffix)
    && same(audit, [`AUDIT ${kind} ABC12345 amount=500.00`]);
}

check(1, 'CREATED sends the confirmation e-mail and writes one audit line', () => {
  const outbox = [];
  const audit = [];
  new BookingEventPublisher([new EmailBookingObserver(outbox), new AuditLogBookingObserver(audit)])
    .publish(event(BookingEventKind.CREATED));
  return same(outbox, [{ to: 'jane@example.com', subject: 'Booking Confirmation - Fleeman',
    body: 'Dear Jane Doe, your booking ABC12345 (Sedan) has been confirmed.' }])
    && same(audit, ['AUDIT CREATED ABC12345 amount=500.00']);
});
check(2, 'MODIFIED sends the update e-mail and writes one audit line',
  () => emailAndAudit(BookingEventKind.MODIFIED, 'Booking Updated - Fleeman', 'has been updated.'));
check(3, 'CANCELLED sends the cancellation e-mail and writes one audit line',
  () => emailAndAudit(BookingEventKind.CANCELLED, 'Booking Cancelled - Fleeman', 'has been cancelled.'));
check(4, 'observers are notified in subscription order', () => {
  const calls = [];
  new BookingEventPublisher([recorder('A', calls), recorder('B', calls), recorder('C', calls)])
    .publish(event(BookingEventKind.CREATED));
  return same(calls, ['A:CREATED', 'B:CREATED', 'C:CREATED']);
});
check(5, 'subscribing the same observer twice notifies it once', () => {
  const calls = [];
  const a = recorder('A', calls);
  const publisher = new BookingEventPublisher([a]);
  publisher.subscribe(a);
  publisher.publish(event(BookingEventKind.CREATED));
  return same(calls, ['A:CREATED']);
});
check(6, 'an unsubscribed observer is no longer notified', () => {
  const calls = [];
  const b = recorder('B', calls);
  const publisher = new BookingEventPublisher([recorder('A', calls), b, recorder('C', calls)]);
  publisher.unsubscribe(b);
  publisher.publish(event(BookingEventKind.CANCELLED));
  return same(calls, ['A:CANCELLED', 'C:CANCELLED']);
});
check(7, 'a throwing observer is logged and the others still run', () => {
  const calls = [];
  const thrower = { onBookingEvent: () => { throw new Error('SMTP down'); } };
  const publisher = new BookingEventPublisher([recorder('A', calls), thrower, recorder('C', calls)]);
  publisher.publish(event(BookingEventKind.CREATED));
  return same(calls, ['A:CREATED', 'C:CREATED'])
    && same(publisher.warnings(), ['observer failed for CREATED ABC12345: SMTP down']);
});
check(8, 'no customer e-mail: nothing sent, audit still written', () => {
  const outbox = [];
  const audit = [];
  new BookingEventPublisher([new EmailBookingObserver(outbox), new AuditLogBookingObserver(audit)])
    .publish(event(BookingEventKind.CREATED, null));
  return outbox.length === 0 && audit.length === 1;
});

console.log(`PASS ${passed}/8`);
process.exit(passed === 8 ? 0 : 1);
