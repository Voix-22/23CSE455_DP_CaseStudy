import java.util.ArrayList;
import java.util.List;
import java.util.function.BooleanSupplier;

// Runs the 8 cases of ../common_spec.md. No test framework.
public class TestRunner {
    private static int passed = 0;

    private static void check(int n, String description, BooleanSupplier test) {
        boolean ok;
        try {
            ok = test.getAsBoolean();
        } catch (RuntimeException ex) {
            ok = false;
        }
        System.out.println((ok ? "PASS " : "FAIL ") + n + " " + description);
        if (ok) passed++;
    }

    private static BookingEvent event(BookingEventKind kind, String email) {
        return new BookingEvent(kind, "ABC12345", "Jane Doe", email, "Sedan", 500.0);
    }

    private static BookingEvent event(BookingEventKind kind) { return event(kind, "jane@example.com"); }

    private static BookingEventObserver recorder(String name, List<String> calls) {
        return e -> calls.add(name + ":" + e.kind());
    }

    // Publishes one event to [email, audit] and checks subject, body suffix and audit line.
    private static boolean emailAndAudit(BookingEventKind kind, String subject, String bodySuffix) {
        List<Email> outbox = new ArrayList<>();
        List<String> audit = new ArrayList<>();
        new BookingEventPublisher(List.of(new EmailBookingObserver(outbox), new AuditLogBookingObserver(audit)))
            .publish(event(kind));
        return outbox.size() == 1
            && outbox.get(0).to().equals("jane@example.com")
            && outbox.get(0).subject().equals(subject)
            && outbox.get(0).body().endsWith(bodySuffix)
            && audit.equals(List.of("AUDIT " + kind + " ABC12345 amount=500.00"));
    }

    public static void main(String[] args) {
        check(1, "CREATED sends the confirmation e-mail and writes one audit line", () -> {
            List<Email> outbox = new ArrayList<>();
            List<String> audit = new ArrayList<>();
            new BookingEventPublisher(List.of(new EmailBookingObserver(outbox), new AuditLogBookingObserver(audit)))
                .publish(event(BookingEventKind.CREATED));
            return outbox.equals(List.of(new Email("jane@example.com", "Booking Confirmation - Fleeman",
                    "Dear Jane Doe, your booking ABC12345 (Sedan) has been confirmed.")))
                && audit.equals(List.of("AUDIT CREATED ABC12345 amount=500.00"));
        });
        check(2, "MODIFIED sends the update e-mail and writes one audit line",
            () -> emailAndAudit(BookingEventKind.MODIFIED, "Booking Updated - Fleeman", "has been updated."));
        check(3, "CANCELLED sends the cancellation e-mail and writes one audit line",
            () -> emailAndAudit(BookingEventKind.CANCELLED, "Booking Cancelled - Fleeman", "has been cancelled."));
        check(4, "observers are notified in subscription order", () -> {
            List<String> calls = new ArrayList<>();
            new BookingEventPublisher(List.of(recorder("A", calls), recorder("B", calls), recorder("C", calls)))
                .publish(event(BookingEventKind.CREATED));
            return calls.equals(List.of("A:CREATED", "B:CREATED", "C:CREATED"));
        });
        check(5, "subscribing the same observer twice notifies it once", () -> {
            List<String> calls = new ArrayList<>();
            BookingEventObserver a = recorder("A", calls);
            BookingEventPublisher publisher = new BookingEventPublisher(List.of(a));
            publisher.subscribe(a);
            publisher.publish(event(BookingEventKind.CREATED));
            return calls.equals(List.of("A:CREATED"));
        });
        check(6, "an unsubscribed observer is no longer notified", () -> {
            List<String> calls = new ArrayList<>();
            BookingEventObserver b = recorder("B", calls);
            BookingEventPublisher publisher = new BookingEventPublisher(List.of(recorder("A", calls), b, recorder("C", calls)));
            publisher.unsubscribe(b);
            publisher.publish(event(BookingEventKind.CANCELLED));
            return calls.equals(List.of("A:CANCELLED", "C:CANCELLED"));
        });
        check(7, "a throwing observer is logged and the others still run", () -> {
            List<String> calls = new ArrayList<>();
            BookingEventObserver thrower = e -> { throw new IllegalStateException("SMTP down"); };
            BookingEventPublisher publisher = new BookingEventPublisher(List.of(recorder("A", calls), thrower, recorder("C", calls)));
            publisher.publish(event(BookingEventKind.CREATED));
            return calls.equals(List.of("A:CREATED", "C:CREATED"))
                && publisher.warnings().equals(List.of("observer failed for CREATED ABC12345: SMTP down"));
        });
        check(8, "no customer e-mail: nothing sent, audit still written", () -> {
            List<Email> outbox = new ArrayList<>();
            List<String> audit = new ArrayList<>();
            new BookingEventPublisher(List.of(new EmailBookingObserver(outbox), new AuditLogBookingObserver(audit)))
                .publish(event(BookingEventKind.CREATED, null));
            return outbox.isEmpty() && audit.size() == 1;
        });
        System.out.println("PASS " + passed + "/8");
        System.exit(passed == 8 ? 0 : 1);
    }
}
