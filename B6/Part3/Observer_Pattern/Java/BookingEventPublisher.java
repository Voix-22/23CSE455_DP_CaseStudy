// Observer pattern - Booking Lifecycle Notification (port of CH04, C# FlemanApi/Service/Notifications).
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;

enum BookingEventKind { CREATED, MODIFIED, CANCELLED }

// Push-model event: the subject sends all the state the observers need.
record BookingEvent(BookingEventKind kind, String confirmationNo, String customerName,
                    String customerEmail, String carTypeName, double estimatedAmount) { }

// Observer
interface BookingEventObserver {
    void onBookingEvent(BookingEvent event);
}

// Subject
interface BookingEventSubject {
    void subscribe(BookingEventObserver observer);
    void unsubscribe(BookingEventObserver observer);
    void publish(BookingEvent event);
}

record Email(String to, String subject, String body) { }

// ConcreteSubject: notifies in subscription order; one failing observer is
// recorded as a warning and does not stop the others.
public class BookingEventPublisher implements BookingEventSubject {
    private final List<BookingEventObserver> observers = new ArrayList<>();
    private final List<String> warnings = new ArrayList<>();

    public BookingEventPublisher(List<? extends BookingEventObserver> initialObservers) {
        for (BookingEventObserver observer : initialObservers) subscribe(observer);
    }

    @Override
    public void subscribe(BookingEventObserver observer) {
        // Identity check, not equals(): the same object is subscribed once.
        for (BookingEventObserver existing : observers) if (existing == observer) return;
        observers.add(observer);
    }

    @Override
    public void unsubscribe(BookingEventObserver observer) {
        observers.removeIf(existing -> existing == observer);
    }

    @Override
    public void publish(BookingEvent event) {
        for (BookingEventObserver observer : List.copyOf(observers)) {
            try {
                observer.onBookingEvent(event);
            } catch (RuntimeException ex) {
                warnings.add("observer failed for " + event.kind() + " " + event.confirmationNo() + ": " + ex.getMessage());
            }
        }
    }

    public List<String> warnings() { return warnings; }
}

// ConcreteObserver: e-mails the customer.
final class EmailBookingObserver implements BookingEventObserver {
    private final List<Email> outbox;

    EmailBookingObserver(List<Email> outbox) { this.outbox = outbox; }

    @Override
    public void onBookingEvent(BookingEvent e) {
        if (e.customerEmail() == null || e.customerEmail().isEmpty()) return;
        String subject = switch (e.kind()) {
            case CREATED -> "Booking Confirmation - Fleeman";
            case MODIFIED -> "Booking Updated - Fleeman";
            case CANCELLED -> "Booking Cancelled - Fleeman";
        };
        String verb = switch (e.kind()) {
            case CREATED -> "confirmed";
            case MODIFIED -> "updated";
            case CANCELLED -> "cancelled";
        };
        outbox.add(new Email(e.customerEmail(), subject,
            "Dear " + e.customerName() + ", your booking " + e.confirmationNo()
                + " (" + e.carTypeName() + ") has been " + verb + "."));
    }
}

// ConcreteObserver: one audit line per event.
final class AuditLogBookingObserver implements BookingEventObserver {
    private final List<String> auditLog;

    AuditLogBookingObserver(List<String> auditLog) { this.auditLog = auditLog; }

    @Override
    public void onBookingEvent(BookingEvent e) {
        auditLog.add(String.format(Locale.ROOT, "AUDIT %s %s amount=%.2f", e.kind(), e.confirmationNo(), e.estimatedAmount()));
    }
}
