using FlemanApi.Service.Notifications;
using FlemanApi.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace FlemanApi.Tests.Services;

[TestFixture]
public class BookingEventPublisherTests
{
    private List<string> _calls = null!;
    private ListLogger<BookingEventPublisher> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        _calls = new List<string>();
        _logger = new ListLogger<BookingEventPublisher>();
    }

    private static BookingEvent MakeEvent(BookingEventKind kind = BookingEventKind.CREATED) => new(
        kind, 1, "ABC12345", 1, "Jane Doe", "jane@example.com", "Sedan",
        new DateTime(2026, 8, 10), new DateTime(2026, 8, 15), 500.0, FlemanApi.Models.BookingStatus.CONFIRMED);

    private sealed class RecordingObserver : IBookingEventObserver
    {
        private readonly string _name;
        private readonly List<string> _calls;
        public RecordingObserver(string name, List<string> calls) { _name = name; _calls = calls; }
        public Task OnBookingEventAsync(BookingEvent bookingEvent)
        {
            _calls.Add($"{_name}:{bookingEvent.Kind}");
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingObserver : IBookingEventObserver
    {
        public Task OnBookingEventAsync(BookingEvent bookingEvent) => throw new InvalidOperationException("SMTP down");
    }

    [Test]
    public async Task Constructor_SubscribesInjectedObservers_NotifiesInRegistrationOrder()
    {
        var publisher = new BookingEventPublisher(
            new IBookingEventObserver[] { new RecordingObserver("first", _calls), new RecordingObserver("second", _calls), new RecordingObserver("third", _calls) },
            _logger);

        await publisher.PublishAsync(MakeEvent());

        _calls.Should().Equal("first:CREATED", "second:CREATED", "third:CREATED");
    }

    [Test]
    public async Task Subscribe_AddsObserver_WhichReceivesLaterEvents()
    {
        var publisher = new BookingEventPublisher(Array.Empty<IBookingEventObserver>(), _logger);
        publisher.Subscribe(new RecordingObserver("late", _calls));

        await publisher.PublishAsync(MakeEvent(BookingEventKind.MODIFIED));

        _calls.Should().Equal("late:MODIFIED");
    }

    [Test]
    public async Task Subscribe_SameObserverTwice_IsNotifiedOnce()
    {
        var observer = new RecordingObserver("once", _calls);
        var publisher = new BookingEventPublisher(new IBookingEventObserver[] { observer }, _logger);
        publisher.Subscribe(observer);

        await publisher.PublishAsync(MakeEvent());

        _calls.Should().Equal("once:CREATED");
    }

    [Test]
    public async Task Unsubscribe_RemovesObserver_OthersStillNotified()
    {
        var removed = new RecordingObserver("removed", _calls);
        var kept = new RecordingObserver("kept", _calls);
        var publisher = new BookingEventPublisher(new IBookingEventObserver[] { removed, kept }, _logger);

        publisher.Unsubscribe(removed);
        await publisher.PublishAsync(MakeEvent(BookingEventKind.CANCELLED));

        _calls.Should().Equal("kept:CANCELLED");
    }

    [Test]
    public async Task PublishAsync_ObserverThrows_LogsWarningAndStillNotifiesTheOthers()
    {
        var publisher = new BookingEventPublisher(
            new IBookingEventObserver[] { new RecordingObserver("before", _calls), new ThrowingObserver(), new RecordingObserver("after", _calls) },
            _logger);

        var act = () => publisher.PublishAsync(MakeEvent());

        await act.Should().NotThrowAsync();
        _calls.Should().Equal("before:CREATED", "after:CREATED");
        _logger.Entries.Should().ContainSingle(e => e.Level == LogLevel.Warning)
            .Which.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Test]
    public async Task PublishAsync_NoObservers_DoesNothing()
    {
        var publisher = new BookingEventPublisher(Array.Empty<IBookingEventObserver>(), _logger);

        var act = () => publisher.PublishAsync(MakeEvent());

        await act.Should().NotThrowAsync();
        _logger.Entries.Should().BeEmpty();
    }
}
