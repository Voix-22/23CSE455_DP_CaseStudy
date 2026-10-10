// Runs the 8 cases of ../common_spec.md. No test framework.
#include <functional>
#include <iostream>
#include <stdexcept>

#include "booking_observer.hpp"

using namespace booking;
using Observers = std::vector<std::shared_ptr<BookingEventObserver>>;
using Strings = std::vector<std::string>;

static int passed = 0;

static void check(int n, const std::string& description, const std::function<bool()>& test) {
    bool ok;
    try {
        ok = test();
    } catch (...) {
        ok = false;
    }
    std::cout << (ok ? "PASS " : "FAIL ") << n << " " << description << "\n";
    if (ok) ++passed;
}

static BookingEvent event(BookingEventKind kind, std::optional<std::string> email = "jane@example.com") {
    return {kind, "ABC12345", "Jane Doe", std::move(email), "Sedan", 500.0};
}

class Recorder : public BookingEventObserver {
public:
    Recorder(std::string name, Strings& calls) : name_(std::move(name)), calls_(calls) {}
    void on_booking_event(const BookingEvent& e) override { calls_.push_back(name_ + ":" + to_string(e.kind)); }

private:
    std::string name_;
    Strings& calls_;
};

class Thrower : public BookingEventObserver {
public:
    void on_booking_event(const BookingEvent&) override { throw std::runtime_error("SMTP down"); }
};

static bool ends_with(const std::string& text, const std::string& suffix) {
    return text.size() >= suffix.size() && text.compare(text.size() - suffix.size(), suffix.size(), suffix) == 0;
}

static bool email_and_audit(BookingEventKind kind, const std::string& subject, const std::string& body_suffix) {
    std::vector<Email> outbox;
    Strings audit;
    BookingEventPublisher({std::make_shared<EmailBookingObserver>(outbox), std::make_shared<AuditLogBookingObserver>(audit)})
        .publish(event(kind));
    return outbox.size() == 1 && outbox[0].to == "jane@example.com" && outbox[0].subject == subject
        && ends_with(outbox[0].body, body_suffix)
        && audit == Strings{std::string("AUDIT ") + to_string(kind) + " ABC12345 amount=500.00"};
}

int main() {
    check(1, "CREATED sends the confirmation e-mail and writes one audit line", [] {
        std::vector<Email> outbox;
        Strings audit;
        BookingEventPublisher({std::make_shared<EmailBookingObserver>(outbox), std::make_shared<AuditLogBookingObserver>(audit)})
            .publish(event(BookingEventKind::CREATED));
        return outbox == std::vector<Email>{{"jane@example.com", "Booking Confirmation - Fleeman",
                                             "Dear Jane Doe, your booking ABC12345 (Sedan) has been confirmed."}}
            && audit == Strings{"AUDIT CREATED ABC12345 amount=500.00"};
    });
    check(2, "MODIFIED sends the update e-mail and writes one audit line",
          [] { return email_and_audit(BookingEventKind::MODIFIED, "Booking Updated - Fleeman", "has been updated."); });
    check(3, "CANCELLED sends the cancellation e-mail and writes one audit line",
          [] { return email_and_audit(BookingEventKind::CANCELLED, "Booking Cancelled - Fleeman", "has been cancelled."); });
    check(4, "observers are notified in subscription order", [] {
        Strings calls;
        BookingEventPublisher({std::make_shared<Recorder>("A", calls), std::make_shared<Recorder>("B", calls),
                               std::make_shared<Recorder>("C", calls)})
            .publish(event(BookingEventKind::CREATED));
        return calls == Strings{"A:CREATED", "B:CREATED", "C:CREATED"};
    });
    check(5, "subscribing the same observer twice notifies it once", [] {
        Strings calls;
        auto a = std::make_shared<Recorder>("A", calls);
        BookingEventPublisher publisher({a});
        publisher.subscribe(a);
        publisher.publish(event(BookingEventKind::CREATED));
        return calls == Strings{"A:CREATED"};
    });
    check(6, "an unsubscribed observer is no longer notified", [] {
        Strings calls;
        std::shared_ptr<BookingEventObserver> b = std::make_shared<Recorder>("B", calls);
        BookingEventPublisher publisher({std::make_shared<Recorder>("A", calls), b, std::make_shared<Recorder>("C", calls)});
        publisher.unsubscribe(b);
        publisher.publish(event(BookingEventKind::CANCELLED));
        return calls == Strings{"A:CANCELLED", "C:CANCELLED"};
    });
    check(7, "a throwing observer is logged and the others still run", [] {
        Strings calls;
        BookingEventPublisher publisher({std::make_shared<Recorder>("A", calls), std::make_shared<Thrower>(),
                                         std::make_shared<Recorder>("C", calls)});
        publisher.publish(event(BookingEventKind::CREATED));
        return calls == Strings{"A:CREATED", "C:CREATED"}
            && publisher.warnings() == Strings{"observer failed for CREATED ABC12345: SMTP down"};
    });
    check(8, "no customer e-mail: nothing sent, audit still written", [] {
        std::vector<Email> outbox;
        Strings audit;
        BookingEventPublisher({std::make_shared<EmailBookingObserver>(outbox), std::make_shared<AuditLogBookingObserver>(audit)})
            .publish(event(BookingEventKind::CREATED, std::nullopt));
        return outbox.empty() && audit.size() == 1;
    });
    std::cout << "PASS " << passed << "/8\n";
    return passed == 8 ? 0 : 1;
}
