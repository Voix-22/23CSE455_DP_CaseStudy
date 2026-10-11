using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlemanApi.Service;

public class EmailQueueHostedService : BackgroundService
{
    private readonly IEmailBackgroundQueue _queue;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<EmailQueueHostedService> _logger;

    public EmailQueueHostedService(IEmailBackgroundQueue queue, IEmailSender emailSender, ILogger<EmailQueueHostedService> logger)
    {
        _queue = queue;
        _emailSender = emailSender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var workItem in _queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await workItem(_emailSender, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Background email send failed");
            }
        }
    }
}
