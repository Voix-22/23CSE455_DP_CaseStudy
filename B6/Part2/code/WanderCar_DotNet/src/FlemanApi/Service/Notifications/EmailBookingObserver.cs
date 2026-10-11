namespace FlemanApi.Service.Notifications;

// ConcreteObserver: e-mails the customer through the existing IEmailService
// (and so through the existing background queue). The CREATED text is moved
// unchanged from the baseline BookingService.CreateBookingAsync.
public class EmailBookingObserver : IBookingEventObserver
{
    public const string CreatedSubject = "Booking Confirmation - Fleeman";
    public const string ModifiedSubject = "Booking Updated - Fleeman";
    public const string CancelledSubject = "Booking Cancelled - Fleeman";

    private readonly IEmailService _emailService;

    public EmailBookingObserver(IEmailService emailService)
    {
        _emailService = emailService;
    }

    public async Task OnBookingEventAsync(BookingEvent e)
    {
        // Baseline only e-mailed when the customer row was found.
        if (string.IsNullOrWhiteSpace(e.CustomerEmail)) return;

        var (subject, body) = e.Kind switch
        {
            BookingEventKind.CREATED => (CreatedSubject, BuildCreatedBody(e)),
            BookingEventKind.MODIFIED => (ModifiedSubject, BuildModifiedBody(e)),
            BookingEventKind.CANCELLED => (CancelledSubject, BuildCancelledBody(e)),
            _ => throw new ArgumentOutOfRangeException(nameof(e), e.Kind, "Unknown booking event kind"),
        };

        await _emailService.SendEmailAsync(e.CustomerEmail, subject, body);
    }

    public static string BuildCreatedBody(BookingEvent e) => $"""
        Dear {e.CustomerName},

        Your booking has been confirmed successfully.

        Booking Details
        -------------------------
        Booking No : {e.ConfirmationNo}
        Vehicle Type : {e.CarTypeName}
        Pickup Date : {e.PickupDatetime}
        Return Date : {e.ReturnDatetime}
        Total Amount : ₹{e.EstimatedAmount:F2}

        Thank you for choosing Fleeman.

        Regards,
        Fleeman Team
        """;

    public static string BuildModifiedBody(BookingEvent e) => $"""
        Dear {e.CustomerName},

        Your booking has been updated.

        Updated Booking Details
        -------------------------
        Booking No : {e.ConfirmationNo}
        Vehicle Type : {e.CarTypeName}
        Pickup Date : {e.PickupDatetime}
        Return Date : {e.ReturnDatetime}
        Total Amount : ₹{e.EstimatedAmount:F2}

        If you did not make this change, please contact us.

        Regards,
        Fleeman Team
        """;

    public static string BuildCancelledBody(BookingEvent e) => $"""
        Dear {e.CustomerName},

        Your booking {e.ConfirmationNo} ({e.CarTypeName}, {e.PickupDatetime} to {e.ReturnDatetime}) has been cancelled.

        We hope to see you again soon.

        Regards,
        Fleeman Team
        """;
}
