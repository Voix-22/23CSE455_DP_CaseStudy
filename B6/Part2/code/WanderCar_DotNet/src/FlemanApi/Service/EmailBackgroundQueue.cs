using System.Threading.Channels;

namespace FlemanApi.Service;

// The .NET equivalent of AsyncConfig's "emailExecutor" ThreadPoolTaskExecutor
// (core=5, max=10, queue=100) + EmailService's @Async methods: fire-and-
// forget work queued here runs on EmailQueueHostedService's background loop
// instead of blocking the request thread.
public interface IEmailBackgroundQueue
{
    void Enqueue(Func<IEmailSender, CancellationToken, Task> workItem);
    IAsyncEnumerable<Func<IEmailSender, CancellationToken, Task>> ReadAllAsync(CancellationToken cancellationToken);
}

public class EmailBackgroundQueue : IEmailBackgroundQueue
{
    private readonly Channel<Func<IEmailSender, CancellationToken, Task>> _channel =
        Channel.CreateBounded<Func<IEmailSender, CancellationToken, Task>>(100);

    public void Enqueue(Func<IEmailSender, CancellationToken, Task> workItem) =>
        _channel.Writer.TryWrite(workItem);

    public IAsyncEnumerable<Func<IEmailSender, CancellationToken, Task>> ReadAllAsync(CancellationToken cancellationToken) =>
        _channel.Reader.ReadAllAsync(cancellationToken);
}
