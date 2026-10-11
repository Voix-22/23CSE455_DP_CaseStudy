namespace FlemanApi.Service.Notifications;

// Observer: one reaction to a booking lifecycle event. New reactions are
// added by registering another implementation in Program.cs, without
// editing BookingService.
public interface IBookingEventObserver
{
    Task OnBookingEventAsync(BookingEvent bookingEvent);
}
