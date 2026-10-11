using Microsoft.Extensions.Logging;

namespace FlemanApi.Tests.TestHelpers;

// Minimal ILogger<T> that records every entry, including the structured
// properties, so tests can assert on what was logged without mocking the
// generic Log<TState> call.
public sealed class ListLogger<T> : ILogger<T>
{
    public sealed record Entry(LogLevel Level, string Message, Exception? Exception, IReadOnlyDictionary<string, object?> Properties);

    public List<Entry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> pairs
            ? pairs.ToDictionary(p => p.Key, p => p.Value)
            : new Dictionary<string, object?>();
        Entries.Add(new Entry(logLevel, formatter(state, exception), exception, properties));
    }
}
