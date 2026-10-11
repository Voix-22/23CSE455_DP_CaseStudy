namespace FlemanApi.Tests.TestHelpers;

// A clock the test sets by hand. LocalTimeZone is UTC so GetLocalNow()
// returns exactly the wall-clock value passed in, on any machine.
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _now;

    public FixedTimeProvider(DateTime now)
    {
        SetNow(now);
    }

    public void SetNow(DateTime now) =>
        _now = new DateTimeOffset(DateTime.SpecifyKind(now, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
