using Microsoft.Extensions.Logging;

namespace FlemanApi.Service.Notifications;

// ConcreteObserver: one structured log entry per booking event. Uses the
// application's existing logging pipeline; no new table.
public class AuditLogBookingObserver : IBookingEventObserver
{
    private readonly ILogger<AuditLogBookingObserver> _logger;

    public AuditLogBookingObserver(ILogger<AuditLogBookingObserver> logger)
    {
        _logger = logger;
    }

    public Task OnBookingEventAsync(BookingEvent e)
    {
        _logger.LogInformation(
            "Booking audit: {EventKind} confirmationNo={ConfirmationNo} bookingId={BookingId} customerId={CustomerId} status={BookingStatus} estimatedAmount={EstimatedAmount}",
            e.Kind, e.ConfirmationNo, e.BookingId, e.CustomerId, e.BookingStatus, e.EstimatedAmount);
        return Task.CompletedTask;
    }
}
