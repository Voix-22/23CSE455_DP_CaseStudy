using Microsoft.Extensions.Logging;

namespace FlemanApi.Service.Notifications;

// ConcreteSubject. Every observer registered in DI is subscribed on
// construction, in registration order. Observers run one after another; a
// failing observer is logged and skipped, the same swallow-and-log rule the
// baseline applied to the confirmation e-mail, so a notification problem
// never fails the booking request.
public class BookingEventPublisher : IBookingEventPublisher
{
    private readonly List<IBookingEventObserver> _observers = new();
    private readonly ILogger<BookingEventPublisher> _logger;

    public BookingEventPublisher(IEnumerable<IBookingEventObserver> observers, ILogger<BookingEventPublisher> logger)
    {
        _logger = logger;
        foreach (var observer in observers) Subscribe(observer);
    }

    public void Subscribe(IBookingEventObserver observer)
    {
        if (!_observers.Contains(observer)) _observers.Add(observer);
    }

    public void Unsubscribe(IBookingEventObserver observer) => _observers.Remove(observer);

    public async Task PublishAsync(BookingEvent bookingEvent)
    {
        // Copy, so an observer that (un)subscribes during notification
        // cannot break the loop.
        foreach (var observer in _observers.ToList())
        {
            try
            {
                await observer.OnBookingEventAsync(bookingEvent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Booking observer {Observer} failed for {EventKind} {ConfirmationNo}",
                    observer.GetType().Name, bookingEvent.Kind, bookingEvent.ConfirmationNo);
            }
        }
    }
}
