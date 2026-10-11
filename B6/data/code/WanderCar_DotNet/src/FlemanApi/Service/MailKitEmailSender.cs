using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace FlemanApi.Service;

public class MailKitEmailSender : IEmailSender
{
    private readonly MailSettings _settings;
    private readonly ILogger<MailKitEmailSender> _logger;

    public MailKitEmailSender(IOptions<MailSettings> settings, ILogger<MailKitEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendPlainAsync(string to, string subject, string body)
    {
        var message = BuildMessage(to, subject);
        message.Body = new TextPart("plain") { Text = body };
        await SendAsync(message);
    }

    public async Task SendWithAttachmentAsync(string to, string subject, string body, string attachmentName, byte[] attachment)
    {
        var message = BuildMessage(to, subject);
        var builder = new BodyBuilder { TextBody = body };
        builder.Attachments.Add(attachmentName, attachment);
        message.Body = builder.ToMessageBody();
        await SendAsync(message);
    }

    private MimeMessage BuildMessage(string to, string subject)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(_settings.Username) ? "no-reply@wandercar.local" : _settings.Username));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        return message;
    }

    private async Task SendAsync(MimeMessage message)
    {
        if (string.IsNullOrWhiteSpace(_settings.Host) || string.IsNullOrWhiteSpace(_settings.Username))
        {
            // No SMTP credentials configured (e.g. fresh checkout, secrets
            // not yet filled in) — log instead of throwing, matching the
            // best-effort "swallow and continue" behaviour every caller in
            // the Java app already wraps its email sends in.
            _logger.LogInformation("Mail not configured — skipping send of {Subject} to {To}", message.Subject, message.To);
            return;
        }

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port,
            _settings.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }
}
