// Observer pattern - Booking Lifecycle Notification (port of CH04, C# FlemanApi/Service/Notifications).
#pragma once

#include <algorithm>
#include <cstdio>
#include <exception>
#include <memory>
#include <optional>
#include <string>
#include <vector>

namespace booking {

enum class BookingEventKind { CREATED, MODIFIED, CANCELLED };

inline const char* to_string(BookingEventKind kind) {
    switch (kind) {
        case BookingEventKind::CREATED: return "CREATED";
        case BookingEventKind::MODIFIED: return "MODIFIED";
        case BookingEventKind::CANCELLED: return "CANCELLED";
    }
    return "UNKNOWN";
}

// Push-model event: the subject sends all the state the observers need.
struct BookingEvent {
    BookingEventKind kind;
    std::string confirmation_no;
    std::string customer_name;
    std::optional<std::string> customer_email;
    std::string car_type_name;
    double estimated_amount;
};

struct Email {
    std::string to, subject, body;
    bool operator==(const Email& other) const {
        return to == other.to && subject == other.subject && body == other.body;
    }
};

// Observer
class BookingEventObserver {
public:
    virtual ~BookingEventObserver() = default;
    virtual void on_booking_event(const BookingEvent& event) = 0;
};

// Subject. Observers are shared_ptr: the subject keeps them alive while subscribed.
class BookingEventSubject {
public:
    virtual ~BookingEventSubject() = default;
    virtual void subscribe(std::shared_ptr<BookingEventObserver> observer) = 0;
    virtual void unsubscribe(const std::shared_ptr<BookingEventObserver>& observer) = 0;
    virtual void publish(const BookingEvent& event) = 0;
};

// ConcreteSubject: notifies in subscription order; one failing observer is
// recorded as a warning and does not stop the others.
class BookingEventPublisher : public BookingEventSubject {
public:
    explicit BookingEventPublisher(const std::vector<std::shared_ptr<BookingEventObserver>>& observers = {}) {
        for (const auto& observer : observers) subscribe(observer);
    }

    void subscribe(std::shared_ptr<BookingEventObserver> observer) override {
        // Pointer comparison is identity: the same object is subscribed once.
        if (std::find(observers_.begin(), observers_.end(), observer) == observers_.end())
            observers_.push_back(std::move(observer));
    }

    void unsubscribe(const std::shared_ptr<BookingEventObserver>& observer) override {
        observers_.erase(std::remove(observers_.begin(), observers_.end(), observer), observers_.end());
    }

    void publish(const BookingEvent& event) override {
        const auto snapshot = observers_;
        for (const auto& observer : snapshot) {
            try {
                observer->on_booking_event(event);
            } catch (const std::exception& ex) {
                warnings_.push_back(std::string("observer failed for ") + to_string(event.kind) + " "
                                    + event.confirmation_no + ": " + ex.what());
            }
        }
    }

    const std::vector<std::string>& warnings() const { return warnings_; }

private:
    std::vector<std::shared_ptr<BookingEventObserver>> observers_;
    std::vector<std::string> warnings_;
};

// ConcreteObserver: e-mails the customer.
class EmailBookingObserver : public BookingEventObserver {
public:
    explicit EmailBookingObserver(std::vector<Email>& outbox) : outbox_(outbox) {}

    void on_booking_event(const BookingEvent& e) override {
        if (!e.customer_email || e.customer_email->empty()) return;
        std::string subject, verb;
        switch (e.kind) {
            case BookingEventKind::CREATED: subject = "Booking Confirmation - Fleeman"; verb = "confirmed"; break;
            case BookingEventKind::MODIFIED: subject = "Booking Updated - Fleeman"; verb = "updated"; break;
            case BookingEventKind::CANCELLED: subject = "Booking Cancelled - Fleeman"; verb = "cancelled"; break;
        }
        outbox_.push_back({*e.customer_email, subject,
                           "Dear " + e.customer_name + ", your booking " + e.confirmation_no + " ("
                               + e.car_type_name + ") has been " + verb + "."});
    }

private:
    std::vector<Email>& outbox_;
};

// ConcreteObserver: one audit line per event.
class AuditLogBookingObserver : public BookingEventObserver {
public:
    explicit AuditLogBookingObserver(std::vector<std::string>& audit_log) : audit_log_(audit_log) {}

    void on_booking_event(const BookingEvent& e) override {
        char amount[32];
        std::snprintf(amount, sizeof amount, "%.2f", e.estimated_amount);
        audit_log_.push_back(std::string("AUDIT ") + to_string(e.kind) + " " + e.confirmation_no + " amount=" + amount);
    }

private:
    std::vector<std::string>& audit_log_;
};

}  // namespace booking
