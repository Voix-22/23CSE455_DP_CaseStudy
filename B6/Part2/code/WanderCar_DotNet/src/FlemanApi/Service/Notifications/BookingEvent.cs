using FlemanApi.Models;

namespace FlemanApi.Service.Notifications;

public enum BookingEventKind { CREATED, MODIFIED, CANCELLED }

// Immutable snapshot of a booking taken right after it was saved, so an
// observer never sees (or changes) the tracked BookingHeader entity.
public sealed record BookingEvent(
    BookingEventKind Kind,
    long BookingId,
    string ConfirmationNo,
    long CustomerId,
    string? CustomerName,
    string? CustomerEmail,
    string? CarTypeName,
    DateTime PickupDatetime,
    DateTime ReturnDatetime,
    double? EstimatedAmount,
    BookingStatus? BookingStatus);
