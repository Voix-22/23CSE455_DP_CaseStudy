namespace FlemanApi.Service.Notifications;

// Subject: keeps the observers and notifies them of booking events.
public interface IBookingEventPublisher
{
    void Subscribe(IBookingEventObserver observer);
    void Unsubscribe(IBookingEventObserver observer);
    Task PublishAsync(BookingEvent bookingEvent);
}
